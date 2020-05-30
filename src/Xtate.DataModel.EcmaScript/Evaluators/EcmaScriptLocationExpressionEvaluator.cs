using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jint.Parser.Ast;
using JintIdentifier = Jint.Parser.Ast.Identifier;

namespace Xtate.DataModel.EcmaScript
{
	internal class EcmaScriptLocationExpressionEvaluator : ILocationEvaluator, ILocationExpression, IAncestorProvider
	{
		private readonly Program?           _declare;
		private readonly Expression?        _leftExpression;
		private readonly LocationExpression _locationExpression;
		private readonly string?            _name;
		private readonly Program            _program;

		public EcmaScriptLocationExpressionEvaluator(in LocationExpression locationExpression, Program program, Expression? leftExpression)
		{
			_locationExpression = locationExpression;
			_program = program;
			_leftExpression = leftExpression;

			switch (leftExpression)
			{
				case null:
					break;

				case JintIdentifier identifier:
					_name = identifier.Name;
					_declare = CreateDeclareStatement(identifier);
					break;

				case MemberExpression memberExpression:
					_name = ((JintIdentifier) memberExpression.Property).Name;
					break;

				default:
					throw new ArgumentOutOfRangeException(nameof(leftExpression));
			}
		}

	#region Interface IAncestorProvider

		object? IAncestorProvider.Ancestor => EcmaScriptHelper.GetAncestor(_locationExpression);

	#endregion

	#region Interface ILocationEvaluator

		public ValueTask<IObject> GetValue(IExecutionContext executionContext, CancellationToken token)
		{
			var obj = new EcmaScriptObject(executionContext.Engine().Eval(_program, startNewScope: true));

			return new ValueTask<IObject>(obj);
		}

		public string GetName(IExecutionContext executionContext) => _name ?? throw new ExecutionException(Resources.Exception_Name_of_Location_Expression_can_t_be_evaluated);

		public ValueTask SetValue(IObject value, IExecutionContext executionContext, CancellationToken token)
		{
			var rightValue = value is EcmaScriptObject ecmaScriptObject ? ecmaScriptObject.JsValue : value.ToObject();
			var assignmentExpression = new AssignmentExpression
									   {
											   Type = SyntaxNodes.AssignmentExpression,
											   Left = _leftExpression,
											   Operator = AssignmentOperator.Assign,
											   Right = new Literal { Type = SyntaxNodes.Literal, Value = rightValue }
									   };

			executionContext.Engine().Exec(assignmentExpression, startNewScope: false);

			return default;
		}

		public void DeclareLocalVariable(IExecutionContext executionContext)
		{
			if (_declare == null)
			{
				throw new ExecutionException(Resources.Exception_InvalidLocalVariableName);
			}

			executionContext.Engine().Exec(_declare, startNewScope: false);
		}

	#endregion

	#region Interface ILocationExpression

		public string? Expression => _locationExpression.Expression;

	#endregion

		private static Program CreateDeclareStatement(JintIdentifier identifier)
		{
			var declarators = new[] { new VariableDeclarator { Id = identifier, Type = SyntaxNodes.VariableDeclarator } };
			var declarations = new[] { new VariableDeclaration { Declarations = declarators, Type = SyntaxNodes.VariableDeclaration } };

			return new Program { VariableDeclarations = declarations, Body = Array.Empty<Statement>(), FunctionDeclarations = Array.Empty<FunctionDeclaration>(), Type = SyntaxNodes.Program };
		}

		public static Expression? GetLeftExpression(Program program)
		{
			if (program.Body.Count != 1)
			{
				return null;
			}

			if (!(program.Body.First() is ExpressionStatement expressionStatement))
			{
				return null;
			}

			return expressionStatement.Expression switch
			{
					JintIdentifier identifier => identifier,
					MemberExpression memberExpression => memberExpression,
					_ => null
			};
		}
	}
}