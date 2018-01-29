namespace Oxide.Plugins
{
  using System.Collections.Generic;

  public partial class Imperium
  {
    class LruCache<K, V>
    {
      Dictionary<K, LinkedListNode<LruCacheItem>> Nodes;
      LinkedList<LruCacheItem> RecencyList;

      public int Capacity { get; private set; }

      public LruCache(int capacity)
      {
        Capacity = capacity;
        Nodes = new Dictionary<K, LinkedListNode<LruCacheItem>>();
        RecencyList = new LinkedList<LruCacheItem>();
      }

      public bool TryGetValue(K key, out V value)
      {
        LinkedListNode<LruCacheItem> node;

        if (!Nodes.TryGetValue(key, out node))
        {
          value = default(V);
          return false;
        }

        LruCacheItem item = node.Value;
        RecencyList.Remove(node);
        RecencyList.AddLast(node);

        value = item.Value;
        return true;
      }

      public void Set(K key, V value)
      {
        LinkedListNode<LruCacheItem> node;

        if (Nodes.TryGetValue(key, out node))
        {
          RecencyList.Remove(node);
          node.Value.Value = value;
        }
        else
        {
          if (Nodes.Count >= Capacity)
            Evict();

          var item = new LruCacheItem(key, value);
          node = new LinkedListNode<LruCacheItem>(item);
        }

        RecencyList.AddLast(node);
        Nodes[key] = node;
      }

      public bool Remove(K key)
      {
        LinkedListNode<LruCacheItem> node;

        if (!Nodes.TryGetValue(key, out node))
          return false;

        Nodes.Remove(key);
        RecencyList.Remove(node);

        return true;
      }

      public void Clear()
      {
        Nodes.Clear();
        RecencyList.Clear();
      }

      void Evict()
      {
        LruCacheItem item = RecencyList.First.Value;
        RecencyList.RemoveFirst();
        Nodes.Remove(item.Key);
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
      }
    }
  }
}
