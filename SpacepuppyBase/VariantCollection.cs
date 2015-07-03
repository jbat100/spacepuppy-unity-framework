﻿using UnityEngine;
using System.Collections.Generic;
using System.Linq;

using com.spacepuppy.Dynamic;

namespace com.spacepuppy
{

    [System.Serializable()]
    public class VariantCollection : IDynamic, ISerializationCallbackReceiver
    {

        #region Fields

        [System.NonSerialized()]
        private Dictionary<string, VariantReference> _table = new Dictionary<string,VariantReference>();

        [SerializeField()]
        private string[] _keys;
        [SerializeField()]
        private VariantReference[] _values;

        #endregion

        #region Properties

        public object this[string key]
        {
            get
            {
                VariantReference v;
                if (_table.TryGetValue(key, out v))
                {
                    return v.Value;
                }
                else
                {
                    return null;
                }
            }
            set
            {
                VariantReference v;
                if (_table.TryGetValue(key, out v))
                {
                    v.Value = value;
                }
                else
                {
                    _table.Add(key, new VariantReference(value));
                }
            }
        }

        public IEnumerable<string> Names { get { return _table.Keys; } }

        #endregion

        #region Methods

        public object GetValue(string key)
        {
            return this[key];
        }

        public void SetValue(string key, object value)
        {
            this[key] = value;
        }

        public VariantReference GetVariant(string key)
        {
            return _table[key];
        }

        public bool HasMember(string key)
        {
            return _table.ContainsKey(key);
        }

        public bool Remove(string key)
        {
            return _table.Remove(key);
        }

        #endregion

        #region IDynamic Interface

        object IDynamic.this[string sMemberName]
        {
            get { return this[sMemberName]; }
            set { this[sMemberName] = value; }
        }

        bool IDynamic.SetValue(string sMemberName, object value, params object[] index)
        {
            this[sMemberName] = value;
            return true;
        }

        object IDynamic.GetValue(string sMemberName, params object[] args)
        {
            return this[sMemberName];
        }

        object IDynamic.InvokeMethod(string sMemberName, params object[] args)
        {
            //throw new System.NotSupportedException();
            return null;
        }

        bool IDynamic.HasMember(string sMemberName, bool includeNonPublic)
        {
            return _table.ContainsKey(sMemberName);
        }

        IEnumerable<System.Reflection.MemberInfo> IDynamic.GetMembers(bool includeNonPublic)
        {
            foreach(var key in _table.Keys)
            {
                yield return new DynamicPropertyInfo(key);
            }
        }

        #endregion

        #region ISerializationCallbackReceiver Interface

        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            _table.Clear();
            var cnt = Mathf.Min(_values.Length, _keys.Length);
            for(int i = 0; i < cnt; i++)
            {
                _table.Add(_keys[i], _values[i]);
            }

            _keys = null;
            _values = null;
        }

        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
            _keys = _table.Keys.ToArray();
            _values = _table.Values.ToArray();
        }

        #endregion



        #region Special Types

        public class EditorHelper : System.Collections.IList
        {

            private VariantCollection _coll;

            #region Properties

            public VariantCollection Collection { get { return _coll; } }

            public int Count { get { return (_coll == null) ? 0 : _coll._table.Count; } }

            public System.Type EntryType { get { return typeof(KeyValuePair<string, VariantReference>); } }

            #endregion

            #region Methods

            public void UpdateCollection(VariantCollection coll)
            {
                _coll = coll;
            }

            public string GetNameAt(int index)
            {
                if (_coll == null) return null;

                if (index < 0 || index >= _coll._table.Count) throw new System.IndexOutOfRangeException();
                int i = 0;
                foreach (var key in _coll._table.Keys)
                {
                    if (i == index) return key;
                    i++;
                }
                return null;
            }

            public VariantReference GetVariant(string key)
            {
                if (_coll == null) return null;

                return _coll.GetVariant(key);
            }

            public VariantReference AddEntry()
            {
                if (_coll == null) return null;

                int cnt = _coll._table.Count + 1;
                string key = "Entry " + cnt.ToString();
                while(_coll._table.ContainsKey(key))
                {
                    cnt++;
                    key = "Entry " + cnt.ToString();
                }

                var v = new VariantReference();
                _coll._table.Add(key, v);
                return v;
            }

            public bool ChangeEntryName(string name, string newName)
            {
                if (_coll == null) return false;
                if (string.IsNullOrEmpty(newName)) return false;

                var names = _coll._table.Keys.ToArray();
                int index = System.Array.IndexOf(names, name);
                if (index < 0 || names.Contains(newName)) return false;

                var values = _coll._table.Values.ToArray();
                _coll._table.Clear();
                names[index] = newName;

                for(int i = 0; i < names.Length; i++)
                {
                    _coll._table.Add(names[i], values[i]);
                }
                return true;
            }

            #endregion


            #region IList Interface

            int System.Collections.IList.Add(object value)
            {
                if (_coll == null) return -1;
                var key = System.Convert.ToString(value);
                if (_coll._table.ContainsKey(key)) return -1;

                _coll._table.Add(key, new VariantReference());
                return _coll._table.Count - 1;
            }

            void System.Collections.IList.Clear()
            {
                if (_coll == null) return;

                _coll._table.Clear();
            }

            bool System.Collections.IList.Contains(object value)
            {
                if (_coll == null) return false;
                return _coll.HasMember(System.Convert.ToString(value));
            }

            int System.Collections.IList.IndexOf(object value)
            {
                if(_coll == null) return -1;

                string svalue = System.Convert.ToString(value);
                int i = 0;
                foreach(var key in _coll._table.Keys)
                {
                    if (key == svalue) return i;
                    i++;
                }
                return -1;
            }

            void System.Collections.IList.Insert(int index, object value)
            {
                throw new System.NotSupportedException();
            }

            bool System.Collections.IList.IsFixedSize
            {
                get { return false; }
            }

            bool System.Collections.IList.IsReadOnly
            {
                get { return false; }
            }

            void System.Collections.IList.Remove(object value)
            {
                if (_coll == null) return;
                var key = System.Convert.ToString(value);
                _coll._table.Remove(key);
            }

            void System.Collections.IList.RemoveAt(int index)
            {
                if (_coll == null) return;

                var key = this.GetNameAt(index);
                _coll.Remove(key);
            }

            object System.Collections.IList.this[int index]
            {
                get
                {
                    return this.GetNameAt(index);
                }
                set
                {
                    if (_coll == null) return;
                    string key = this.GetNameAt(index);
                    this.ChangeEntryName(key, System.Convert.ToString(value));
                }
            }

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
                if (_coll == null) return Enumerable.Empty<string>().GetEnumerator();

                return _coll._table.Keys.GetEnumerator();
            }

            void System.Collections.ICollection.CopyTo(System.Array array, int index)
            {
                if (_coll == null) return;

                (_coll._table.Keys as System.Collections.ICollection).CopyTo(array, index);
            }

            int System.Collections.ICollection.Count
            {
                get { return (_coll == null) ? 0 : _coll._table.Count; }
            }

            bool System.Collections.ICollection.IsSynchronized
            {
                get { return false; }
            }

            object System.Collections.ICollection.SyncRoot
            {
                get { return this; }
            }

            #endregion

        }

        #endregion

    }

}
