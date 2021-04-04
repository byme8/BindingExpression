using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace BindingExpression
{
    public static class ExpressionExtensions
    {
        public static string GetBindingPath<TValue>(this Expression<Func<TValue>> expression)
        {
            var path = GetPath(expression.Body).ToArray();

            if (!path.Any())
            {
                throw new NotSupportedException($"Expressions like '{expression.ToString()}' is not supported.");
            }
            
            if (path.Length == 1)
            {
                return ".";
            }

            return string.Join(".", path.Reverse().Skip(1));

            IEnumerable<string> GetPath(Expression body)
            {
                var currentBody = body;
                while (currentBody is MemberExpression member)
                {
                    yield return member.Member.Name;
                    currentBody = member.Expression;
                }
            }
        }
    }
}