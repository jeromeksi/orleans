using System.Buffers;
using System.Text;

namespace Orleans.Storage
{
    /// <summary>
    /// Orleans v3-compatible hasher implementation for string-only grain key ids.
    /// </summary>
    internal class Orleans3CompatibleStringKeyHasher(Orleans3CompatibleHasher innerHasher, string grainType) : IHasher
    {
        /// <summary>
        /// <see cref="IHasher.Description"/>
        /// </summary>
        public string Description { get; } = $"Orleans v3 hash function ({nameof(JenkinsHash)}).";

        /// <summary>
        /// <see cref="IHasher.Hash(byte[])"/>.
        /// </summary>
        public int Hash(byte[] data)
        {
            // Orleans v3 treats string-only keys as integer keys with extension (AdoGrainKey.IsLongKey == true),
            // so data must be extended for string-only grain keys.
            // But AdoNetGrainStorage implementation also uses such code:
            //    ...
            //    var grainIdHash = HashPicker.PickHasher(serviceId, this.name, baseGrainType, grainReference, grainState).Hash(grainId.GetHashBytes());
            //    var grainTypeHash = HashPicker.PickHasher(serviceId, this.name, baseGrainType, grainReference, grainState).Hash(Encoding.UTF8.GetBytes(baseGrainType));
            //    ...
            // PickHasher parameters are the same for both calls so we need to analyze data content to distinguish these cases.
            // It doesn't word if string key is equal to grain type name, but we consider this edge case to be negligibly rare.

            if (IsGrainTypeName(data))
                return innerHasher.Hash(data);

            var extendedLength = data.Length + 8;

            const int maxOnStack = 256;
            byte[] rentedBuffer = null;

            // assuming code below never throws, so calling ArrayPool.Return without try/finally block for JIT optimization

            var buffer = extendedLength > maxOnStack
                ? (rentedBuffer = ArrayPool<byte>.Shared.Rent(extendedLength)).AsSpan()
                : stackalloc byte[maxOnStack];

            buffer = buffer[..extendedLength];

            data.AsSpan().CopyTo(buffer);
            // buffer may contain arbitrary data, setting zeros in 'extension' segment
            buffer[data.Length..].Clear();

            var hash = innerHasher.Hash(buffer);

            if (rentedBuffer is not null)
                ArrayPool<byte>.Shared.Return(rentedBuffer);

            return hash;
        }

        private bool IsGrainTypeName(byte[] data)
        {
            // at least 1 byte per char
            if (data.Length < grainType.Length)
                return false;

            var grainTypeByteCount = Encoding.UTF8.GetByteCount(grainType);
            if (grainTypeByteCount != data.Length)
                return false;

            const int maxOnStack = 256;
            byte[] rentedBuffer = null;

            // assuming code below never throws, so calling ArrayPool.Return without try/finally block for JIT optimization

            var buffer = grainTypeByteCount > maxOnStack
                ? (rentedBuffer = ArrayPool<byte>.Shared.Rent(grainTypeByteCount)).AsSpan()
                : stackalloc byte[maxOnStack];

            buffer = buffer[..grainTypeByteCount];

            var bytesWritten = Encoding.UTF8.GetBytes(grainType, buffer);
            var isGrainType = buffer[..bytesWritten].SequenceEqual(data);
            if (rentedBuffer is not null)
                ArrayPool<byte>.Shared.Return(rentedBuffer);

            return isGrainType;
        }
    }
}
