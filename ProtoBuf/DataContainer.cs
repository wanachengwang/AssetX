using UnityEngine;
using System.Collections;

using System.Collections.Generic;
using System;

using System.Reflection;


public class DataContainer<TID, TItem>
    where TID : IComparable
    where TItem : ProtoBuf.IExtensible
{
    static DataContainer<TID, TItem> _instance;
    public static DataContainer<TID, TItem> Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = new DataContainer<TID, TItem>();
            }
            return _instance;
        }
    }
    Dictionary<TID, TItem> _items;
    public Dictionary<TID, TItem> Items
    {
        get
        {
            if (_items == null)
            {
                _items = InitData();
            }
            return _items;
        }
        set
        {
            _items = value;
        }
    }
    protected Dictionary<TID, TItem> InitData()
    {
        Dictionary<TID, TItem> items = new Dictionary<TID, TItem>();
        try
        {
            string itemArrayTypeName = typeof(TItem).FullName + "_ARRAY";
            Type itemArrayType = Type.GetType(itemArrayTypeName);
            System.Object array = DataAccess.Deserializer(itemArrayType);
            var getItemsMethod = itemArrayType.GetMethod("get_items");
            var list = getItemsMethod.Invoke(array, null) as List<TItem>;
            var getIDMethod = typeof(TItem).GetMethod("get_ID");
            var itr = list.GetEnumerator();
            while (itr.MoveNext())
            {
                TID ID = (TID)getIDMethod.Invoke(itr.Current, null);
                items[ID] = itr.Current;
            }
            return items;
        }
        catch (System.Exception e)
        {
            Debug.LogError(typeof(TItem).Name + e);
            return items;
        }
    }
    List<TID> _IDs;
    public List<TID> IDs
    {
        get
        {
            if (_IDs == null)
            {
                _IDs = new List<TID>(Items.Keys);
            }
            return _IDs;
        }
        private set
        {
            _IDs = value;
        }
    }
    public bool Contains(TID ID)
    {
        return Items.ContainsKey(ID);
    }
    public TItem this[TID ID]
    {
        get
        {
            if (!Items.ContainsKey(ID))
            {
                Debug.LogError(string.Format("{0} ID:{1} 不存在", typeof(TItem).Name, ID));
                return default(TItem);
            }
            //throw new System.Exception("invalid " + typeof(TItem).Name + " ID : " + ID);
            return Items[ID];
        }
    }
}

