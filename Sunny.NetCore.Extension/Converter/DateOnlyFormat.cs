using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Text.Json;
using System.Threading.Tasks;

namespace Sunny.NetCore.Extension.Converter
{
	public sealed partial class DateOnlyFormat : System.Text.Json.Serialization.JsonConverter<DateOnly>
	{
		public static DateOnlyFormat Singleton { get; private set; }
		private DateOnlyFormat() {
			// 由于循环依赖，所以只能先传出指针
			Singleton = this;
			var datetimef = DateTimeFormat.Singleton;
			ShortChar01 = RefExtractVector128(in datetimef.ShortChar0);
			ShortD1 = RefExtractVector128(datetimef.ShortD);
			ShortBit2X10Vector1 = RefExtractVector128(datetimef.ShortBit2X10Vector);
			Int101 = RefExtractVector128(datetimef.Int10);
			Short101 = RefExtractVector128(datetimef.Short10);
			SbyteMax1 = RefExtractVector128(datetimef.SbyteMax);
			ShortN151 = RefExtractVector128(datetimef.ShortN15);
		}
		[MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
		private static ref Vector128<T> RefExtractVector128<T>(in Vector256<T> vector256) where T : struct
		{
			return ref Unsafe.As<Vector256<T>, Vector128<T>>(ref Unsafe.AsRef(in vector256));
		}
		[MethodImpl(MethodImplOptions.AggressiveOptimization)]
		public override unsafe DateOnly Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		{
			var s = reader.ValueSpan;
			if (TryParseDateOnly(s, out var value)) return value;
			throw new ArgumentException("DateTime的格式不正确");
		}
		[MethodImpl(MethodImplOptions.AggressiveOptimization)]
		public override unsafe void Write(Utf8JsonWriter writer, DateOnly value, JsonSerializerOptions options)
		{
			var r = DateToUtf8_10(value);
			writer.WriteStringValue(new ReadOnlySpan<byte>(&r, 10));
		}
		[MethodImpl(MethodImplOptions.AggressiveOptimization)]
		public unsafe string DateOnlyToString(DateOnly value)
		{
			var str = AsciiInterface.FastAllocateString(10);
			var vector = Avx2.ConvertToVector256Int16(DateToUtf8_10(value));
			AsciiInterface.StringTo<char, Vector128<short>>(str) = vector.GetLower();
			Unsafe.Add(ref AsciiInterface.StringTo<char, int>(str), 4) = vector.AsInt32().GetElement(4);
			return str;
		}
		[MethodImpl(MethodImplOptions.AggressiveOptimization)]
		public unsafe bool TryFormat(DateOnly value, Span<char> destination, out int charsWritten)
		{
			charsWritten = 10;
			if (destination.Length < 10) return false;
			var vector = Avx2.ConvertToVector256Int16(DateToUtf8_10(value));
			AsciiInterface.StringTo<char, Vector128<short>>(destination) = vector.GetLower();
			Unsafe.Add(ref AsciiInterface.StringTo<char, int>(destination), 4) = vector.AsInt32().GetElement(4);
			return true;
		}
		[MethodImpl(MethodImplOptions.AggressiveOptimization)]
		public unsafe bool TryParse(string str, out DateOnly result)
		{
			var vector = AsciiInterface.UnicodeToAscii_32(ref AsciiInterface.StringTo<char, Vector256<short>>(str));
			return TryParseDateOnly(new ReadOnlySpan<byte>(&vector, str.Length), out result);
		}
		[MethodImpl(MethodImplOptions.AggressiveOptimization)]
		public unsafe bool TryParse(ReadOnlySpan<char> s, out DateOnly result)
		{
			var vector = AsciiInterface.UnicodeToAscii_32(ref AsciiInterface.StringTo<char, Vector256<short>>(s));
			return TryParseDateOnly(new ReadOnlySpan<byte>(&vector, s.Length), out result);
		}
		[MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
		private unsafe bool TryParseDateOnly(ReadOnlySpan<byte> input, out DateOnly value)
		{
			Unsafe.SkipInit(out value);
			bool success = true;
			if (input.Length == 10 | input.Length == 11)
			{
				var v128 = AsciiInterface.StringTo<byte, Vector128<sbyte>>(input);
				if (IsNumber(v128, input.Length))
				{
					//纯数字时表示时间戳
					System.Buffers.Text.Utf8Parser.TryParse(input, out long lv, out _);
					value = new DateOnly(1970, 1, 1).AddDays((int)(TimeSpan.TicksPerSecond * lv / TimeSpan.TicksPerDay));
					return true;
				}
				return Utf8_10ToDate(in v128, out value);
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
				value = new DateOnly(year, month, day);
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
		static DateOnlyFormat()
		{
			new DateOnlyFormat();
		}
		internal readonly Vector128<short> ShortChar01;
		internal readonly Vector128<short> ShortD1;
		internal readonly Vector128<short> ShortBit2X10Vector1;
		private readonly Vector128<sbyte> SByteChat01 = Vector128.Create((sbyte)'0');
		private readonly Vector128<sbyte> SByteN15 = Vector128.Create((sbyte)~15);
		private readonly AsciiInterface AsciiInterface = AsciiInterface.Singleton;

		[MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
		private unsafe Vector128<byte> DateToUtf8_10(DateOnly value)
		{
			var yyyy = value.Year;
			var yt = yyyy / 100;
			var MM = value.Month;
			var dd = value.Day;
			var numbers = Vector128.Create(yt, yyyy - yt * 100, MM, dd); //JIT内部优化
			var v = Ssse3.Shuffle(Sse41.Insert(NumberToUtf8Bit2(numbers), (sbyte)'-', 8), TUShuffleMask1).AsByte(); //寄存器优化
			return v;
		}
		//最多输入4个数字，输出8个结果，每个数字最大值不能超过255。
		[MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
		private Vector128<sbyte> NumberToUtf8Bit2(Vector128<int> numbers)
		{
			var vector = Sse2.And(Sse2.ShiftRightLogical(Sse2.MultiplyLow(Sse2.And(Sse2.Or(Sse2.ShiftLeftLogical(numbers, 16), numbers).AsInt16(), this.SbyteMax1), this.ShortBit2X10Vector1), 7), this.SbyteMax1);
			vector = Sse2.Add(Sse2.Subtract(vector, Sse2.MultiplyLow(Sse2.And(Sse2.ShiftRightLogical(Sse2.MultiplyLow(vector, ShortD1), 7), this.SbyteMax1), this.Short101)), this.ShortChar01);
			return Sse2.PackSignedSaturate(vector, vector);
		}
		internal readonly Vector128<short> Short101;
		internal readonly Vector128<short> SbyteMax1;
		private readonly Vector128<sbyte> TUShuffleMask1 = Vector128.Create(0, 1, 2, 3, 8, 4, 5, 8, 6, 7, -1, -1, -1, -1, -1, -1);

		[MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
		internal unsafe bool Utf8_10ToDate(in Vector128<sbyte> input, out DateOnly value)
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
			value = new DateOnly(year, month, day);    //寄存器优化
			return true;
		}
		internal readonly Vector128<short> Int101;
		private readonly Vector128<sbyte> TDShuffleMask1 = Vector128.Create(0, 1, 2, 3, 5, 6, 8, 9, -1, -1, -1, -1, -1, -1, -1, -1);
		private readonly Vector128<short> Max = Vector128.Create(9900, 99, 12, 31, 24, 60, 60, 0);
		private readonly Vector128<short> Min = Vector128.Create(0, 0, 1, 1, 0, 0, 0, 0);
		internal readonly Vector128<short> ShortN151;
	}
}
