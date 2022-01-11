using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Sunny.NetCore.Extension.Converter
{
    public sealed class DateTimeFormat : System.Text.Json.Serialization.JsonConverter<DateTime>
    {
		public static DateTimeFormat Singleton { get; private set; }
		private DateTimeFormat()
        {
			// 由于循环依赖，所以只能先传出指针
			Singleton = this;
#if NET6_0_OR_GREATER
			DateOnlyF = DateOnlyFormat.Singleton;
#else
			ShortChar01 = ShortChar0.GetLower();
			Int101 = Int10.GetLower();
			ShortN151 = ShortN15.GetLower();
#endif
		}
		[MethodImpl(MethodImplOptions.AggressiveOptimization)]
		public override unsafe DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		{
			var s = reader.ValueSpan;
			if (TryParseDateTime(s, out var value)) return value;
			throw new JsonException();
		}
		[MethodImpl(MethodImplOptions.AggressiveOptimization)]
		public override unsafe void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
		{
			var r = DateTimeToUtf8_19(value);
			writer.WriteStringValue(new ReadOnlySpan<byte>(&r, 19));
		}
		[MethodImpl(MethodImplOptions.AggressiveOptimization)]
		public unsafe string DateTimeToString(DateTime value)
		{
			var str = AsciiInterface.FastAllocateString(19);
			var r = DateTimeToUtf8_19(value);
			Set38(Avx2.ConvertToVector256Int16(Avx2.ExtractVector128(r, 0)), Avx2.ConvertToVector256Int16(Avx2.ExtractVector128(r, 1)), ref Unsafe.AsRef(in str.GetPinnableReference()));
			return str;
		}
		[MethodImpl(MethodImplOptions.AggressiveOptimization)]
		public unsafe bool TryFormat(DateTime value, Span<char> destination, out int charsWritten)
		{
			charsWritten = 19;
			if (destination.Length < 19) return false;
			var r = DateTimeToUtf8_19(value);
			Set38(Avx2.ConvertToVector256Int16(Avx2.ExtractVector128(r, 0)), Avx2.ConvertToVector256Int16(Avx2.ExtractVector128(r, 1)), ref Unsafe.AsRef(in destination.GetPinnableReference()));
			return true;
		}
		[MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
		private static void Set38(Vector256<short> vector0, Vector256<short> vector1, ref char output)
		{
			Unsafe.As<char, Vector256<short>>(ref output) = vector0;
			Unsafe.Add(ref Unsafe.As<char, int>(ref output), 8) = vector1.AsInt32().GetElement(0);
			Unsafe.Add(ref Unsafe.As<char, short>(ref output), 18) = vector1.GetElement(2);
		}
		[MethodImpl(MethodImplOptions.AggressiveOptimization)]
		public unsafe bool TryParse(string str, out DateTime result)
		{
			var vector = AsciiInterface.UnicodeToAscii_32(ref AsciiInterface.StringTo<char, Vector256<short>>(str));
			return TryParseDateTime(new ReadOnlySpan<byte>(&vector, str.Length), out result);
		}
		[MethodImpl(MethodImplOptions.AggressiveOptimization)]
		public unsafe bool TryParse(ReadOnlySpan<char> s, out DateTime result)
		{
			var vector = AsciiInterface.UnicodeToAscii_32(ref AsciiInterface.StringTo<char, Vector256<short>>(s));
			return TryParseDateTime(new ReadOnlySpan<byte>(&vector, s.Length), out result);
		}
		[MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
		private unsafe bool TryParseDateTime(ReadOnlySpan<byte> input, out DateTime value)
		{
#if NET5_0_OR_GREATER
			Unsafe.SkipInit(out value);
#else
			value = default;
#endif
			bool success = true;
			if (input.Length == 19 | input.Length == 20) return Utf8_19ToDate(in AsciiInterface.StringTo<byte, Vector256<sbyte>>(input), out value);
			if (input.Length == 10 | input.Length == 11)
			{
				var v128 = AsciiInterface.StringTo<byte, Vector128<sbyte>>(input);
				if (IsNumber(v128, input.Length))
				{
					//纯数字时表示时间戳
					System.Buffers.Text.Utf8Parser.TryParse(input, out long lv, out _);
					value = new DateTime(1970, 1, 1).AddTicks(TimeSpan.TicksPerSecond * lv);
					return true;
				}
#if NET6_0_OR_GREATER
				var r = DateOnlyF.Utf8_10ToDate(v128, out var v0);
				value = v0.ToDateTime(default);
#else
				var r = Utf8_10ToDate(v128, out value);
#endif
				return r;
			}
			if (input.Length < 10 & input.Length > 7)
			{
				int start = 0, length = 4;
				success &= System.Buffers.Text.Utf8Parser.TryParse(input.Slice(start, length), out int year, out _);
				start += length + 1;
				length = 0;
				while (length != 2 & input[start + length] != '-') ++length;
				success &= System.Buffers.Text.Utf8Parser.TryParse(input.Slice(start, length), out int month, out _);
				start += length + 1;
				length = 0;
				while ((start + length != input.Length) && (length != 2 & input[start + length] != '-')) ++length;
				success &= System.Buffers.Text.Utf8Parser.TryParse(input.Slice(start, length), out int day, out _);
				if (!success)
				{
					return false;
				}
				value = new DateTime(year, month, day);
				return true;
			}
			return false;
		}
		[MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
		private bool IsNumber(Vector128<sbyte> input, int length)
		{
			var success = Sse2.MoveMask(Sse2.CompareEqual(Sse2.And(Sse2.Subtract(input, SByteChat01), SByteN15), Vector128<sbyte>.Zero));
			//这里本来应该用TZCNT，但.NET不支持
			var test = (1 << length) - 1;
			return test == (success & test);
		}
		static DateTimeFormat()
		{
			new DateTimeFormat();
		}
		internal readonly Vector256<short> ShortChar0 = Vector256.Create((short)'0');
		internal readonly Vector256<short> ShortD = Vector256.Create((short)0xD);   //D CD CCCD
																		  //7 11 19 分别最大值255 2047 524287
																		  //最大化精度需要容器：short int long
		internal readonly Vector256<short> ShortBit2X10Vector = Vector256.Create(0xD, 0x80, 0xD, 0x80, 0xD, 0x80, 0xD, 0x80, 0xD, 0x80, 0xD, 0x80, 0xD, 0x80, 0xD, 0x80);
		private readonly Vector128<sbyte> SByteChat01 = Vector128.Create((sbyte)'0');
		private readonly Vector128<sbyte> SByteN15 = Vector128.Create((sbyte)~15);
		private readonly AsciiInterface AsciiInterface = AsciiInterface.Singleton;
#if NET6_0_OR_GREATER
		private readonly DateOnlyFormat DateOnlyF;
#else
		[MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
		internal unsafe bool Utf8_10ToDate(Vector128<sbyte> input, out DateTime value)
		{
			var vector = Sse2.Subtract(Sse41.ConvertToVector128Int16(Ssse3.Shuffle(input, TDShuffleMask1)), this.ShortChar01);
			var v = Ssse3.HorizontalAdd(Sse2.MultiplyLow(vector, this.Int101), Vector128<short>.Zero).AsUInt16();
			var v2 = Avx.Or(Sse2.Or(Sse2.CompareLessThan(Max, v.AsInt16()), Sse2.CompareLessThan(v.AsInt16(), Min)).AsInt16(), Sse2.And(vector, this.ShortN151));     //此处最大宽度128，使用or比使用insert更快
			if (!Sse41.TestZ(v2, v2))
			{
				value = default;
				return false;
			}
			var year = Sse2.Extract(v, 0) + Sse2.Extract(v, 1);
			var month = Sse2.Extract(v, 2);
			var day = Sse2.Extract(v, 3);
			value = new DateTime(year, month, day);    //寄存器优化
			return true;
		}
		private readonly Vector128<sbyte> TDShuffleMask1 = Vector128.Create(0, 1, 2, 3, 5, 6, 8, 9, -1, -1, -1, -1, -1, -1, -1, -1);
		private readonly Vector128<short> ShortChar01;
		private readonly Vector128<short> Int101;
		private readonly Vector128<short> ShortN151;
#endif

		[MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
		private unsafe Vector256<byte> DateTimeToUtf8_19(DateTime value)
		{
			var yyyy = value.Year;
			var yt = yyyy / 100;
			var MM = value.Month;
			var dd = value.Day;
			var hh = value.Hour;
			var mm = value.Minute;
			var ss = value.Second;
			Vector256<int> numbers = Vector256.Create(yt, yyyy - yt * 100, MM, dd, hh, mm, ss, yt); //JIT内部优化
			var v128 = NumberToUtf8Bit2(numbers);
			Vector256<sbyte> vector = Vector256.Create(
				Sse41.Insert(Sse41.Insert(Sse41.Insert(v128, (sbyte)'-', 12), (sbyte)' ', 13), (sbyte)':', 14),
				Sse41.Insert(Sse2.Insert(v128.AsUInt16(), Sse2.Extract(v128.AsUInt16(), 6), 0).AsSByte(), (sbyte)':', 2));
			var v = Avx2.Shuffle(vector, TUShuffleMask).AsByte();
			return v;
		}
		//最多输入8个数字，输出16个结果，每个数字最大值不能超过255。
		[MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
		private Vector128<sbyte> NumberToUtf8Bit2(Vector256<int> numbers)
		{
			var vector = Avx2.And(Avx2.ShiftRightLogical(Avx2.MultiplyLow(Avx2.And(Avx2.Or(numbers, Avx2.ShiftLeftLogical(numbers, 16)).AsInt16(), SbyteMax), ShortBit2X10Vector), 7), SbyteMax);
			vector = Avx2.Add(Avx2.Subtract(vector, Avx2.MultiplyLow(Avx2.And(Avx2.ShiftRightLogical(Avx2.MultiplyLow(vector, ShortD), 7), SbyteMax), Short10)), ShortChar0);
			return Sse2.PackSignedSaturate(Avx2.ExtractVector128(vector, 0), Avx2.ExtractVector128(vector, 1));
		}
		internal readonly Vector256<short> Short10 = Vector256.Create((short)10);
		internal readonly Vector256<short> SbyteMax = Vector256.Create((short)sbyte.MaxValue);
		private readonly Vector256<sbyte> TUShuffleMask = Vector256.Create(0, 1, 2, 3, 12, 4, 5, 12, 6, 7, 13, 8, 9, 14, 10, 11, 2, 0, 1, -1, -1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);

		[MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
		private unsafe bool Utf8_19ToDate(in Vector256<sbyte> input, out DateTime value)
		{
			var vector = Avx2.Subtract(Avx2.ConvertToVector256Int16(Avx2.ExtractVector128(Avx2.PermuteVar8x32(Avx2.Shuffle(input, TDShuffleMask).AsInt32(), Permute), 0).AsSByte()), ShortChar0);
			var v0 = Avx2.MultiplyLow(vector, Int10).AsInt16();
			//不能使用256位指令，因为256位指令的顺序与128位不同
			var v = Ssse3.HorizontalAdd(v0.GetLower(), Avx2.ExtractVector128(v0, 1)).AsUInt16();    //双数位置的乘数为0，所以不用担心short溢出
			var v2 = Avx2.Or(Avx.InsertVector128(Sse2.CompareLessThan(Max, v.AsInt16()).ToVector256Unsafe(), Sse2.CompareLessThan(v.AsInt16(), Min), 1), Avx2.And(vector, ShortN15));   //有一个原始数组的长度是256，使用insert比使用or减少1个extract指令
			if (!Avx.TestZ(v2, v2))
			{
				value = default;
				return false;
			}
			var year = Sse2.Extract(v, 0) + Sse2.Extract(v, 1);
			var month = Sse2.Extract(v, 2);
			var day = Sse2.Extract(v, 3);
			var hour = Sse2.Extract(v, 4);
			var minu = Sse2.Extract(v, 5);
			var seco = Sse2.Extract(v, 6);
			value = new DateTime(year, month, day, hour, minu, seco);    //寄存器优化
			return true;
		}
		internal readonly Vector256<short> Int10 = Vector256.Create(1000, 100, 10, 1, 10, 1, 10, 1, 10, 1, 10, 1, 10, 1, 0, 0);
		private readonly Vector256<sbyte> TDShuffleMask = Vector256.Create(0, 1, 2, 3, 5, 6, 8, 9, 11, 12, 14, 15, -1, -1, -1, -1, 1, 2, 1, 2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1);
		private readonly Vector256<int> Permute = Vector256.Create(0, 1, 2, 4, 3, 3, 3, 3);
		internal readonly Vector256<short> ShortN15 = Vector256.Create((sbyte)~15).AsInt16();
		private readonly Vector128<short> Max = Vector128.Create(9900, 99, 12, 31, 24, 60, 60, 0);
		private readonly Vector128<short> Min = Vector128.Create(0, 0, 1, 1, 0, 0, 0, 0);
	}
}
