using System.Collections;
using System;
using System.Reflection;
using ProtoBuf;
using UnityEngine;
using AssetX;

public class DataAccess
{
    public static TArray Deserializer<TArray, TPathAccessor>()
        where TArray : IExtensible
        where TPathAccessor : IExtensible
    {
        Type type = typeof(TPathAccessor);
        ConstructorInfo ctor = type.GetConstructor(Type.EmptyTypes);
        object obj = ctor.Invoke(null);
        var method = type.GetMethod("get_dataPath");
        var dataPath = method.Invoke(obj, null) as string;
        var text = AssetX.AssetManager.Instance.LoadAsset<TextAsset>(dataPath);
        TArray t = ProtoBuf.Serializer.Deserialize<TArray>(new System.IO.MemoryStream(text.bytes));
        //ResourceManager.Instance.UnloadAsset(dataPath);
        return t;
    }

    public static System.Object Deserializer(Type arrayType)
    {
        if (!typeof(IExtensible).IsAssignableFrom(arrayType))
            throw new System.ArgumentException("type : " + arrayType.FullName + " is not inherited form ProtoBuf.IExtensible");

        string pathAccessorTypeName = arrayType.FullName + "_DATAPATH";
        Type pathAccessorType = Type.GetType(pathAccessorTypeName);
        if (!typeof(IExtensible).IsAssignableFrom(pathAccessorType))
            throw new System.ArgumentException("type : " + arrayType.FullName + " is not inherited form ProtoBuf.IExtensible");

        ConstructorInfo ctor = pathAccessorType.GetConstructor(Type.EmptyTypes);
        object obj = ctor.Invoke(null);
        var method = pathAccessorType.GetMethod("get_dataPath");
        var dataPath = method.Invoke(obj, null) as string;

        var text = AssetX.AssetManager.Instance.LoadAsset<TextAsset>(dataPath);
        System.Object array = ProtoBuf.Serializer.NonGeneric.Deserialize(arrayType, new System.IO.MemoryStream(text.bytes));
        //ResourceManager.Instance.UnloadAsset(dataPath);
        return array;
    }
}
