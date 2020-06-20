using System;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace GeohashTest
{
    public class Geohash
    {
        const byte MAX_BIT_PRECISION = 64;
        const byte MAX_CHARACTER_PRECISION = 12;
        
        //纬度,y
        private ulong latBits;
        //经度,x
        private ulong lngBits;
        private byte significantBits;

        private Geohash()
        {
            //_mm256_xor_si256
            //Avx2.Xor()
            
            //_mm256_and_si256
            //Avx2.And()

            //_mm256_set1_epi64x
            // var x = Vector256.Create(222).AsByte();
            // Avx2.Or(x, x);

            //_mm256_storeu_si256
            //Avx.Store();
            
            //https://stackoverflow.com/questions/58628817/how-to-use-dotnet-core-3-intrinsic-storealignednontemporal

            //_mm256_slli_epi16
            //Avx2.ShiftLeftLogical()

            //_mm256_srli_epi16
            //Avx2.ShiftRightLogical()

            //_mm256_lddqu_si256
            //Avx2.LoadDquVector256()

            //_mm256_shuffle_epi8
            //Avx2.Shuffle()
        }

        public static Vector256<ulong> EncodeIntSIMD(Vector256<double> latVec, Vector256<double> lngVec)
        {
            // Quantize.
            // var exampleLatArr = new double[] {116, 115, 114, 113};
            // var exampleLatLoad =
            //     Vector256.Create(exampleLatArr[0], exampleLatArr[1], exampleLatArr[2], exampleLatArr[3]);
            // __m256d latq = _mm256_loadu_pd(lat);
            
            // latq = _mm256_mul_pd(latq, _mm256_set1_pd(1/180.0));
            var latq= Avx.Multiply(latVec, Vector256.Create(1 / 180).AsDouble());

            // latq = _mm256_add_pd(latq, _mm256_set1_pd(1.5));
            latq= Avx.Add(latq, Vector256.Create(1.5).AsDouble());
            
            // __m256i lati = _mm256_srli_epi64(_mm256_castpd_si256(latq), 20);
            var lati = Avx2.ShiftRightLogical(latq.AsUInt64(), 20);

            // __m256d lngq = _mm256_loadu_pd(lng);
            
            // lngq = _mm256_mul_pd(lngq, _mm256_set1_pd(1/360.0));
            var lngq = Avx.Multiply(lngVec, Vector256.Create(1 / 360).AsDouble());
            
            // lngq = _mm256_add_pd(lngq, _mm256_set1_pd(1.5));
            lngq = Avx.Add(lngq, Vector256.Create(1.5).AsDouble());
            
            // __m256i lngi = _mm256_srli_epi64(_mm256_castpd_si256(lngq), 20);
            var lngi = Avx2.ShiftRightLogical(lngq.AsUInt64(), 20);

            // Spread.
            // __m256i hash = _mm256_or_si256(spread(lati), _mm256_slli_epi64(spread(lngi), 1));
            Vector256<ulong> hash = Avx2.Or(SpreadSIMD(lati.AsByte()),
                Avx2.ShiftRightLogical(SpreadSIMD(lngi.AsByte()), 1));
            
            // _mm256_storeu_si256((__m256i *)output, hash);
            return hash;
        }

        private static Vector256<ulong> SpreadSIMD(Vector256<byte> x)
        {
            // x  = _mm256_shuffle_epi8(x, _mm256_set_epi8(
            // -1, 11, -1, 10, -1, 9, -1, 8,
            // -1, 3, -1, 2, -1, 1, -1, 0,
            // -1, 11, -1, 10, -1, 9, -1, 8,
            // -1, 3, -1, 2, -1, 1, -1, 0));
            //the order of Vector256.Create is reversed of _mm256_set_epi8!
            
            x = Avx2.Shuffle(x.AsSByte(), Vector256.Create(
                0,-1,1,-1, 2,-1, 3,-1,
                8,-1,9,-1,10,-1,11,-1,
                0,-1,1,-1, 2,-1, 3,-1,
                8,-1,9,-1,10,-1,11,-1)).AsByte();

            // const __m256i lut = _mm256_set_epi8(
            // 85, 84, 81, 80, 69, 68, 65, 64,
            // 21, 20, 17, 16, 5, 4, 1, 0,
            // 85, 84, 81, 80, 69, 68, 65, 64,
            // 21, 20, 17, 16, 5, 4, 1, 0);
            // Vector256<byte> lut = Vector256.Create(
            //     (byte)0,1,4,5,16,17,20,21,
            //     64,65,68,69,80,81,84,85,
            //     0,1,4,5,16,17,20,21,
            //     64,65,68,69,80,81,84,85
            // );
            Vector256<byte> lut = Vector256.Create(
          (byte)0b00000000,0b00000001,0b00000100,0b00000101,0b00010000,0b00010001,0b00010100,0b00010101,
                0b01000000,0b01000001,0b01000100,0b01000101,0b01010000,0b01010001,0b01010100,0b01010101,
                0b00000000,0b00000001,0b00000100,0b00000101,0b00010000,0b00010001,0b00010100,0b00010101,
                0b01000000,0b01000001,0b01000100,0b01000101,0b01010000,0b01010001,0b01010100,0b01010101
            );

            // __m256i lo = _mm256_and_si256(x, _mm256_set1_epi8(0xf));
            Vector256<byte> lo = Avx2.And(x, Vector256.Create((byte)0x0f));
            
            // lo = _mm256_shuffle_epi8(lut, lo);
            lo=Avx2.Shuffle(lut, lo);

            // __m256i hi = _mm256_and_si256(x, _mm256_set1_epi8(0xf0));
            var hi = Avx2.And(x, Vector256.Create((byte) 0xf0));
            
            // hi = _mm256_shuffle_epi8(lut, _mm256_srli_epi64(hi, 4));
            hi = Avx2.Shuffle(lut, Avx2.ShiftRightLogical(hi.AsUInt64(), 4).AsByte());

            // x = _mm256_or_si256(lo, _mm256_slli_epi64(hi, 8));
            x = Avx2.Or(lo, Avx2.ShiftRightLogical(hi.AsUInt64(), 8).AsByte());

            return x.AsUInt64();
        }

        public static ulong EncodeInt(double lat, double lng)
        {
            return Interleave(QuantizeLngBits(lng, 32), QuantizeLatBits(lat, 32));
        }

        public static ulong EncodeIntPDEP(double lat, double lng)
        {
            return InterleavePDEP(QuantizeLngBits(lng, 32), QuantizeLatBits(lat, 32));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ulong InterleavePDEP(uint x, uint y)
        {
            return Bmi2.X64.ParallelBitDeposit(x, 0xaaaa_aaaa_aaaa_aaaaL) | Bmi2.X64.ParallelBitDeposit(y, 0x5555_5555_5555_5555L);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ulong Interleave(uint x,uint y)
        {
            return (Spread(x) << 1) | Spread(y);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong Spread(uint x)
        {
            ulong X = x;
            X = (X | (X << 16)) & 0x0000_ffff_0000_ffffL;
            X = (X | (X << 8))  & 0x00ff_00ff_00ff_00ffL;
            X = (X | (X << 4))  & 0x0f0f_0f0f_0f0f_0f0fL;
            X = (X | (X << 2))  & 0x3333333333333333L;
            X = (X | (X << 1))  & 0x5555555555555555L;
            return X;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static uint QuantizeLngBits(double lng,byte latLen)
        {
            return QuantizeBits(latLen, lng / 360.0 + 1.5);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static uint QuantizeLatBits(double lat,byte latLen)
        {
            return QuantizeBits(latLen, lat / 180.0 + 1.5);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static uint QuantizeBits(byte latLen, double value)
        {
            //20 + (32 - latLen);
            return (uint) ((DoubleToUInt64Bits(value) & 0x00_0F_FF_FF_FF_FF_FF_FFL) >> (52 - latLen));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe ulong DoubleToUInt64Bits(double value)
        {
            return *(ulong*) &value;
        }
    }
}