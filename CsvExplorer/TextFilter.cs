namespace CsvExplorer
{
    public abstract class Filter
    {
        public abstract bool Matches(object value);
    }

    public abstract class Filter<T> : Filter
    {
        public abstract bool Matches<TValue>(TValue value) where TValue : T;

        public override bool Matches(object value)
        {
            return Matches((T)value);
        }
    }

    public class TextFilter : Filter<string>
    {
        private string Pattern { get; }

        public TextFilter(string pattern)
        {
            Pattern = pattern;
        }

        public override bool Matches<TValue>(TValue value)
        {
            return value.ToLower().Contains(Pattern.ToLower());
        }
    }
}
