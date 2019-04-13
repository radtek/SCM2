using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Swift
{
    /// <summary>
    /// Dictionary with KeyArray and ValueArray cached which guarantee the iteration order
    /// </summary>
    public class StableDictionary<TKey, TValue>
    {
        TKey[] keyArr = null;
        TValue[] valueArr = null;
        List<TKey> keyLst = new List<TKey>();
        List<TValue> valueLst = new List<TValue>();
        Dictionary<TKey, TValue> dict = new Dictionary<TKey, TValue>();

        public TValue this[TKey key]
        {
            get
            {
                return dict[key];
            }
            set
            {
                bool newKey = !dict.ContainsKey(key);
                dict[key] = value;

                if (newKey)
                {
                    keyArr = null;
                    valueArr = null;
                    keyLst.Add(key);
                    valueLst.Add(value);
                }
            }
        }

        public void Clear()
        {
            keyArr = null;
            valueArr = null;
            dict.Clear();
            keyLst.Clear();
            valueLst.Clear();
        }

        public IEnumerable<TKey> Keys
        {
            get
            {
                return keyLst;
            }
        }

        public TKey[] KeyArray
        {
            get
            {
                if (keyArr == null)
                    keyArr = keyLst.ToArray();

                return keyArr;
            }
        }

        public IEnumerable<TValue> Values
        {
            get
            {
                return valueLst;
            }
        }

        public TValue[] ValueArray
        {
            get
            {
                if (valueArr == null)
                    valueArr = valueLst.ToArray();

                return valueArr;
            }
        }

        public bool ContainsKey(TKey key)
        {
            return dict.ContainsKey(key);
        }

        public bool Remove(TKey key)
        {
            if (!dict.ContainsKey(key))
                return false;

            var v = dict[key];
            dict.Remove(key);
            keyLst.Remove(key);
            valueLst.Remove(v);
            keyArr = null;
            valueArr = null;

            return true;
        }

        public int Count
        {
            get
            {
                return KeyArray.Length;
            }
        }
    }
}