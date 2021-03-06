#pragma warning disable CS0169,CS0649,IDE0044,IDE0051
using System.Collections.Generic;
using Xunit;

namespace Lexico.Test
{
    public class RepeatTests
    {
        private struct Comma
        {
            [Literal(",")] private Unnamed _;
        }

        private class NonGenericRepeat
        {
            [Literal("(")] private Unnamed _open;
            [field: SeparatedBy(typeof(Comma))] public List<int> Values { get; private set; }
            [Literal(")")] private Unnamed _close;
        }

        [Fact]
        public void RepeatParser() => Assert.Equal(Lexico.Parse<NonGenericRepeat>("(1, 1, 3, 5, 7)").Values, new []{1, 1, 3, 5, 7});

        private class GenericRepeat<T>
        {
            [Literal("(")] private Unnamed _open;
            [field: SeparatedBy(typeof(Comma))] public List<T> Values { get; private set; }
            [Literal(")")] private Unnamed _close;
        }

        [Fact]
        public void GenericRepeatParser() => Assert.Equal(Lexico.Parse<GenericRepeat<int>>("(1, 1, 3, 5, 7)").Values, new []{1, 1, 3, 5, 7});

        [SurroundBy("[", "]")]
        private class OptionalRepeat
        {
            [Optional] public NonGenericRepeat List;
        }

        [Fact]
        public void OptionalRepeatOfNothingReturnsNull() => Assert.Null(Lexico.Parse<OptionalRepeat>("[]").List);
    }
}