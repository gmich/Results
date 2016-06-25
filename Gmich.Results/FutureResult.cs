using System;
using System.Linq.Expressions;

namespace Gmich.Results
{

    public class FutureResult
    {
        private Expression<Func<Result>> resultExpression;

        public Result Result => resultExpression.Compile().Invoke();

        private FutureResult(Expression<Func<Result>> getter)
        {
            resultExpression = getter;
        }

        public static FutureResult For(Expression<Func<Result>> getter)
        {
            return new FutureResult(getter);
        }

        private FutureResult BinaryExpressionConverter(
            Expression<Func<Result>> left,
            Expression<Func<Result>> right,
            Func<Expression, Expression, BinaryExpression> converter)
        {
            BinaryExpression expression = converter(left.Body, right.Body);

            return new FutureResult(Expression.Lambda<Func<Result>>(expression));
        }

        public FutureResult And(FutureResult other)
        {
            return BinaryExpressionConverter(resultExpression, other.resultExpression, BinaryExpression.AndAlso);
        }

        public FutureResult Or(FutureResult other)
        {
            return BinaryExpressionConverter(resultExpression, other.resultExpression, BinaryExpression.OrElse);
        }

        public FutureResult Not
        {
            get
            {
                var notExpression = Expression.Not(resultExpression.Body);
                resultExpression = Expression.Lambda<Func<Result>>(notExpression);
                return this;
            }
        }
    }
}