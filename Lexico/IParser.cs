using System;
using System.Linq.Expressions;
using static System.Linq.Expressions.Expression;

namespace Lexico
{
    /// <summary>
    /// A stateless object that consumes text and turns it into values
    /// </summary>
    public interface IParser
    {
        Type OutputType { get; }
        void Compile(ICompileContext context);
    }
}