using System.Collections.ObjectModel;

namespace SheeToList.Utils
{
    public class Grouping<TKey, TItem> : ObservableCollection<TItem>
    {
        public TKey Key { get; }

        public Grouping(TKey key, IEnumerable<TItem> items) : base(items)
        {
            Key = key;
        }

        public override string ToString() => Key?.ToString() ?? base.ToString();
    }
}