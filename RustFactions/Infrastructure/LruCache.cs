namespace Oxide.Plugins
{
  using System.Collections.Generic;

  public partial class RustFactions
  {
    class LruCache<K, V>
    {
      Dictionary<K, LinkedListNode<LruCacheItem>> Nodes = new Dictionary<K, LinkedListNode<LruCacheItem>>();
      LinkedList<LruCacheItem> RecencyList = new LinkedList<LruCacheItem>();

      public int MaxItems { get; private set; }

      public LruCache(int maxItems)
      {
        MaxItems = maxItems;
      }

      public bool TryGetValue(K key, out V value)
      {
        LinkedListNode<LruCacheItem> node;

        if (!Nodes.TryGetValue(key, out node))
        {
          value = default(V);
          return false;
        }

        value = node.Value.Value;
        RecencyList.Remove(node);
        RecencyList.AddFirst(node);

        return true;
      }

      public void Add(K key, V value)
      {
        if (Nodes.Count >= MaxItems)
          Evict();

        var item = new LruCacheItem(key, value);
        var node = new LinkedListNode<LruCacheItem>(item);

        Nodes.Add(key, node);
        RecencyList.Remove(item);
        RecencyList.AddFirst(node);
      }

      public void Clear()
      {
        Nodes.Clear();
        RecencyList.Clear();
      }

      void Evict()
      {
        var key = RecencyList.Last.Value.Key;
        RecencyList.RemoveLast();
        Nodes.Remove(key);
      }

      class LruCacheItem
      {
        public K Key;
        public V Value;

        public LruCacheItem(K key, V value)
        {
          Key = key;
          Value = value;
        }

        public override int GetHashCode()
        {
          return Key.GetHashCode();
        }

        public override bool Equals(object obj)
        {
          var other = obj as LruCacheItem;
          return other != null && other.Key.Equals(Key);
        }
      }
    }
  }
}
