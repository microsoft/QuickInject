namespace QuickInject.BuildPlanVisitors
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq.Expressions;

    public class GeneratedCodeTextWriterBuildPlanVisitor : IBuildPlanVisitor
    {
        private readonly Func<Type, TextWriter> writerResolver;

        public GeneratedCodeTextWriterBuildPlanVisitor(Func<Type, TextWriter> writerResolver)
        {
            this.writerResolver = writerResolver;
        }

        public Expression Visitor(Expression expression, Type type)
        {
            var writer = this.writerResolver(type);
            new GeneratedCodeTextWriterExpressionVisitor(this.writerResolver(type), type).Visit(expression);
            writer.Flush();
            writer.Dispose();
            return expression;
        }

        private sealed class GeneratedCodeTextWriterExpressionVisitor : ExpressionVisitor
        {
            private readonly TextWriter writer;

            private readonly Dictionary<ParameterExpression, string> parameters = new Dictionary<ParameterExpression, string>();

            private int indentLevel;

            private bool onNewLine;

            private bool topLevelBlockVisited;

            public GeneratedCodeTextWriterExpressionVisitor(TextWriter writer, Type type)
            {
                this.writer = writer;
                this.writer.WriteLine("static " + type + " Create_" + type + "()");
            }

            protected override Expression VisitBlock(BlockExpression node)
            {
                bool addReturnStatement = !this.topLevelBlockVisited;
                this.topLevelBlockVisited = true;

                this.WriteLineIndented("{");
                this.indentLevel += 4;

                if (node.Variables.Count > 0)
                {
                    foreach (var variable in node.Variables)
                    {
                        var variableName = "var" + this.parameters.Values.Count;
                        this.parameters.Add(variable, variableName);
                        this.WriteLineIndented(variable.Type + " " + variableName + ";");
                    }

                    this.WriteLineIndented("");
                }

                foreach (var expression in node.Expressions)
                {
                    if (expression.NodeType == ExpressionType.Parameter)
                    {
                        continue; // we don't want to print hanging parameter expressions
                    }

                    this.Visit(expression);

                    if (expression.NodeType != ExpressionType.Block && expression.NodeType != ExpressionType.Conditional)
                    {
                        this.WriteIndented(";");
                    }
                    
                    this.WriteLineIndented("");
                }

                if (addReturnStatement)
                {
                    this.WriteLineIndented("return " + this.parameters[node.Variables[node.Variables.Count - 1]] + ";");
                }

                this.indentLevel -= 4;
                this.WriteLineIndented("}");

                return node;
            }

            protected override Expression VisitLambda<T>(Expression<T> node)
            {
                this.WriteIndented("() => ");
                this.Visit(node.Body);
                return node;
            }

            protected override Expression VisitBinary(BinaryExpression node)
            {
                this.Visit(node.Left);

                if (node.NodeType == ExpressionType.Assign)
                {
                    this.writer.Write(" = ");
                }

                if (node.NodeType == ExpressionType.Equal)
                {
                    this.writer.Write(" == ");
                }

                this.Visit(node.Right);

                return node;
            }

            protected override Expression VisitConstant(ConstantExpression node)
            {
                if (node.Value == null)
                {
                    this.WriteIndented("null");
                }

                return node;
            }

            protected override Expression VisitParameter(ParameterExpression node)
            {
                this.WriteIndented(this.parameters[node]);

                return node;
            }

            protected override Expression VisitNew(NewExpression node)
            {
                this.WriteIndented("new " + node.Constructor.DeclaringType + "(");
                var arguments = node.Arguments;
                for (int i = 0; i < arguments.Count; ++i)
                {
                    var argument = arguments[i];

                    if (argument is ParameterExpression)
                    {
                        var parameter = argument as ParameterExpression;
                        this.WriteIndented(this.parameters[parameter]);
                    }
                    else if (argument is ConstantExpression)
                    {
                        var constant = argument as ConstantExpression;
                        if (constant.Value == null)
                        {
                            this.WriteIndented("null");
                        }
                        else if (constant.Type == typeof(string))
                        {
                            this.WriteIndented("\"" + constant.Value + "\"");
                        }
                        else
                        {
                            this.WriteIndented(".Constant<" + constant.Type + ">");
                        }
                    }
                    else
                    {
                        this.Visit(arguments[i]);
                    }

                    if (i != arguments.Count - 1)
                    {
                        this.WriteIndented(", ");
                    }
                }

                this.WriteIndented(")");

                return node;
            }

            protected override Expression VisitConditional(ConditionalExpression node)
            {
                this.WriteIndented("if (");
                this.Visit(node.Test);
                this.writer.WriteLine(")");

                if (!(node.IfTrue is ParameterExpression))
                {
                    this.WriteLineIndented("{");
                    this.indentLevel += 4;
                    this.Visit(node.IfTrue);
                    this.indentLevel -= 4;
                    this.WriteIndented("}");
                }
                else
                {
                    this.WriteLineIndented("{");
                    this.WriteIndented("}");
                }

                if (!(node.IfFalse is ParameterExpression))
                {
                    this.WriteLineIndented("else");
                    this.WriteLineIndented("{");
                    this.indentLevel += 4;
                    this.Visit(node.IfFalse);
                    this.indentLevel -= 4;
                    this.WriteIndented("}");
                }

                return node;
            }

            protected override Expression VisitMethodCall(MethodCallExpression node)
            {
                this.WriteIndented(node.Method.DeclaringType + "." + node.Method.Name + "(");
                var arguments = node.Arguments;
                for (int i = 0; i < arguments.Count; ++i)
                {
                    var argument = arguments[i];

                    if (argument is ParameterExpression)
                    {
                        var parameter = argument as ParameterExpression;
                        this.WriteIndented(this.parameters[parameter]);
                    }
                    else if (argument is ConstantExpression)
                    {
                        var constant = argument as ConstantExpression;
                        if (constant.Value == null)
                        {
                            this.WriteIndented("null");
                        }
                        else if (constant.Type == typeof(string))
                        {
                            this.WriteIndented("\"" + constant.Value + "\"");
                        }
                        else
                        {
                            this.WriteIndented(".Constant<" + constant.Type + ">");
                        }
                    }
                    else
                    {
                        this.Visit(arguments[i]);
                    }

                    if (i != arguments.Count - 1)
                    {
                        this.WriteIndented(", ");
                    }
                }

                this.WriteIndented(")");

                return node;
            }

            protected override Expression VisitUnary(UnaryExpression node)
            {
                if (node.NodeType == ExpressionType.Convert)
                {
                    this.WriteIndented("(" + node.Type + ")");
                }

                this.WriteIndented("(");
                this.Visit(node.Operand);

                if (node.NodeType == ExpressionType.TypeAs)
                {
                    this.WriteIndented(" as " + node.Type);
                }

                this.writer.Write(")");

                return node;
            }

            private void WriteIndented(string str)
            {
                if (this.onNewLine)
                {
                    for (int i = 0; i < this.indentLevel; ++i)
                    {
                        this.writer.Write(" ");
                    }
                }

                this.writer.Write(str);
                this.onNewLine = false;
            }

            private void WriteLineIndented(string str)
            {
                for (int i = 0; i < this.indentLevel; ++i)
                {
                    this.writer.Write(" ");
                }

                this.writer.WriteLine(str);
                this.onNewLine = true;
            }
        }
    }
}