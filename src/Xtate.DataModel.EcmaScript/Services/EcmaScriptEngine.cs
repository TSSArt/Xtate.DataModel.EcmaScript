// Copyright © 2019-2026 Sergii Artemenko
// 
// This file is part of the Xtate project. <https://xtate.net/>
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as published
// by the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Affero General Public License for more details.
// 
// You should have received a copy of the GNU Affero General Public License
// along with this program.  If not, see <https://www.gnu.org/licenses/>.

using System.Globalization;
using Jint.Runtime;
using Jint.Runtime.Descriptors;
using Xtate.DataModel.EcmaScript.Internal;
using Xtate.DataTypes;
using Identifier = Xtate.StateMachine.Identifier;

namespace Xtate.DataModel.EcmaScript.Services;

public class EcmaScriptEngine
{
    public const string LocationValueProperty = @"__xtate_location_value";

    private readonly Engine _jintEngine;

    private readonly Stack<Scope> _scopes = new();

    private readonly HashSet<string> _variableSet = [];

    public EcmaScriptEngine()
    {
        _jintEngine = new Engine(options => options.Culture(CultureInfo.InvariantCulture).LimitRecursion(1024).Strict());

        var global = _jintEngine.Global;

        global.DefineOwnProperty(property: @"In", new PropertyDescriptor(JsValue.FromObject(_jintEngine, new Func<string, bool>(InState)), writable: false, enumerable: false, configurable: false));
        global.DefineOwnProperty(LocationValueProperty, new PropertyDescriptor(JsValue.Undefined, writable: true, enumerable: false, configurable: false));
    }

    public required IDataModelController DataModelController { private get; [SetByIoC] init; }

    public required IInStateController InStateController { private get; [SetByIoC] init; }

    private bool InState(string state) => InStateController.InState((Identifier)state);

    private void SyncRootVariables(DataModelList dataModel)
    {
        var global = _jintEngine.Global;
        List<string>? toRemove = null;

        foreach (var name in _variableSet)
        {
            if (!dataModel.TryGet(name, caseInsensitive: false, out _))
            {
                toRemove ??= [];
                toRemove.Add(name);
            }
        }

        if (toRemove is not null)
        {
            foreach (var property in toRemove)
            {
                _variableSet.Remove(property);
                global.RemoveOwnProperty(property);
            }
        }

        foreach (var keyValue in dataModel.KeyValues)
        {
            if (!string.IsNullOrEmpty(keyValue.Key) && global.GetOwnProperty(keyValue.Key) == PropertyDescriptor.Undefined)
            {
                var descriptor = EcmaScriptHelper.CreatePropertyAccessor(_jintEngine, dataModel, keyValue.Key);
                global.FastSetProperty(keyValue.Key, descriptor);
                _variableSet.Add(keyValue.Key);
            }
        }
    }

    public void EnterExecutionContext()
    {
        var propertyNames = new HashSet<string>();

        foreach (var property in _jintEngine.Global.GetOwnPropertyKeys(Types.String))
        {
            propertyNames.Add(property.AsString());
        }

        _scopes.Push(new Scope(propertyNames));
    }

    public void LeaveExecutionContext()
    {
        var scope = _scopes.Pop();
        var global = _jintEngine.Global;

        foreach (var property in global.GetOwnPropertyKeys(Types.String).ToArray())
        {
            if (!scope.PropertyNames.Contains(property.AsString()))
            {
                global.RemoveOwnProperty(property);
            }
        }

        foreach (var (property, descriptor) in scope.ShadowedProperties)
        {
            global.FastSetProperty(property, descriptor);
        }
    }

    public void DeclareLocalVariable(string name)
    {
        var scope = _scopes.Peek();
        var global = _jintEngine.Global;

        if (!scope.ShadowedProperties.ContainsKey(name))
        {
            var descriptor = global.GetOwnProperty(name);

            if (descriptor != PropertyDescriptor.Undefined)
            {
                scope.ShadowedProperties.Add(name, descriptor);
            }
        }

        scope.LocalVariables.Add(name);
        global.FastSetProperty(name, new PropertyDescriptor(JsValue.Undefined, writable: true, enumerable: true, configurable: true));
    }

    public void SetLocationValue(in Prepared<Script> assignment, string? identifierName, object? value)
    {
        var jsValue = value is JsValue nativeValue ? nativeValue : JsValue.FromObject(_jintEngine, value);

        if (identifierName is not null)
        {
            var localVariable = false;

            foreach (var scope in _scopes)
            {
                if (scope.LocalVariables.Contains(identifierName))
                {
                    localVariable = true;

                    break;
                }
            }

            if (!localVariable)
            {
                DataModelController.DataModel[identifierName, caseInsensitive: false] = EcmaScriptHelper.ConvertFromJsValue(jsValue);
                SyncRootVariables(DataModelController.DataModel);

                return;
            }
        }

        SyncRootVariables(DataModelController.DataModel);
        _jintEngine.Global.Set(LocationValueProperty, jsValue);
        _jintEngine.Execute(assignment);
    }

    public JsValue Eval(in Prepared<Script> program, bool startNewScope)
    {
        SyncRootVariables(DataModelController.DataModel);

        if (!startNewScope)
        {
            return _jintEngine.Evaluate(program);
        }

        EnterExecutionContext();

        try
        {
            return _jintEngine.Evaluate(program);
        }
        finally
        {
            LeaveExecutionContext();
        }
    }

    public void Exec(in Prepared<Script> program, bool startNewScope)
    {
        SyncRootVariables(DataModelController.DataModel);

        if (!startNewScope)
        {
            _jintEngine.Execute(program);

            return;
        }

        EnterExecutionContext();

        try
        {
            _jintEngine.Execute(program);
        }
        finally
        {
            LeaveExecutionContext();
        }
    }

    private sealed class Scope(HashSet<string> propertyNames)
    {
        public HashSet<string> PropertyNames { get; } = propertyNames;

        public Dictionary<string, PropertyDescriptor> ShadowedProperties { get; } = new();

        public HashSet<string> LocalVariables { get; } = [];
    }
}