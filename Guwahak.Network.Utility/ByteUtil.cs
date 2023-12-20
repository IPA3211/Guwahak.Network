using MessagePack.Resolvers;
using MessagePack;
using System.IO.Compression;
using System.Reflection;

namespace Guwahak.Network.Utility
{    // Object 를 ByteArray로 바꿈 압축 포함
    public class ByteUtil
    {
        static bool serializerRegistered;

        static void Initialize()
        {
            if (serializerRegistered)
            {
                return;
            }

            StaticCompositeResolver.Instance.Register(
                StandardResolver.Instance
            );

            var option = MessagePackSerializerOptions.Standard.WithResolver(StaticCompositeResolver.Instance);

            MessagePackSerializer.DefaultOptions = option;
            serializerRegistered = true;
        }

        static byte[] ObjectToByteArrayGeneric<T>(T obj)
        {
            Initialize();
            return Compress(MessagePackSerializer.Serialize(obj));
        }

        public static byte[] ObjectToByteArray<T>(T obj)
        {
            return ObjectToByteArrayGeneric(obj);
        }

        public static byte[] ObjectToByteArray(object obj, Type t)
        {
            var ObjectToByteArraytFunc = typeof(ByteUtil).GetMethod(nameof(ObjectToByteArrayGeneric),
                BindingFlags.NonPublic | BindingFlags.Static);

            if (ObjectToByteArraytFunc == null)
            {
                throw new Exception("Cannot find ByteUtil.ObjectToByteArraytFunc!");
            }

            var genericObjectToByteArrayFunc = ObjectToByteArraytFunc.MakeGenericMethod(t);
            if (genericObjectToByteArrayFunc == null)
            {
                throw new Exception("Cannot find ByteUtil.ByteArrayToObjectByGeneric<T>!");
            }

            return genericObjectToByteArrayFunc.Invoke(null, new object[] { obj }) as byte[];
        }

        public static object ByteArrayToObject(byte[] arrBytes, Type type)
        {
            Initialize();

            // Android, iOS IL2CPP에서는 제네릭이 아닌 MessagePackSerializer.Deserialize() 함수를 쓸 수 없다.
            // (실행 시 ExecutionEngineException 예외 발생)
            // 그래서 일단 리플렉션으로 우회한다.
            return ByteArrayToObjectByReflection(arrBytes, type);
        }

        public static T ByteArrayToObject<T>(byte[] arrBytes)
        {
            Initialize();
            return MessagePackSerializer.Deserialize<T>(Decompress(arrBytes));
        }

        // Unity 프로젝트에서는 ByteArrayToObject 함수를 리플렉션으로 찾아 쓰는데,
        // 이름이 모호하다고 하므로 다른 이름의 함수를 하나 더 만든다.
        static T ByteArrayToObjectByGeneric<T>(byte[] arrBytes)
        {
            return ByteArrayToObject<T>(arrBytes);
        }

        static object ByteArrayToObjectByReflection(byte[] arrBytes, Type t)
        {
            var byteArrayToObjectFunc = typeof(ByteUtil).GetMethod(nameof(ByteArrayToObjectByGeneric),
                BindingFlags.NonPublic | BindingFlags.Static);

            if (byteArrayToObjectFunc == null)
            {
                throw new Exception("Cannot find ByteUtil.ByteArrayToObjectByGeneric!");
            }

            var genericByteArrayToObjectFunc = byteArrayToObjectFunc.MakeGenericMethod(t);
            if (genericByteArrayToObjectFunc == null)
            {
                throw new Exception("Cannot find ByteUtil.ByteArrayToObjectByGeneric<T>!");
            }

            return genericByteArrayToObjectFunc.Invoke(null, new object[] { arrBytes });
        }
        public static byte[] Compress(byte[] data)
        {
            var output = new MemoryStream();
            using (var deflateStream = new DeflateStream(output, CompressionLevel.Optimal))
            {
                deflateStream.Write(data, 0, data.Length);
            }
            return output.ToArray();
        }

        public static byte[] Decompress(byte[] data)
        {
            var input = new MemoryStream(data);
            var output = new MemoryStream();
            using (var deflateStream = new DeflateStream(input, CompressionMode.Decompress))
            {
                deflateStream.CopyTo(output);
            }
            return output.ToArray();
        }
    }
}
