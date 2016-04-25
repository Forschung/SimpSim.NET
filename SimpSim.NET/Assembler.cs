using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace SimpSim.NET
{
    public class Assembler
    {
        private static IDictionary<string, byte> _symbolTable;
        private static InstructionByteCollection _instructionBytes;

        public Assembler()
        {
            _symbolTable = new Dictionary<string, byte>();
            _instructionBytes = new InstructionByteCollection();
        }

        public Instruction[] Assemble(string assemblyCode)
        {
            _symbolTable.Clear();
            _instructionBytes.Reset();

            foreach (string line in assemblyCode.Split(Environment.NewLine.ToCharArray(), StringSplitOptions.RemoveEmptyEntries))
            {
                InstructionSyntax instructionSyntax = InstructionSyntax.Parse(line);

                if (!string.IsNullOrWhiteSpace(instructionSyntax.Label))
                    AddLabelToSymbolTable(instructionSyntax.Label);

                if (!string.IsNullOrWhiteSpace(instructionSyntax.Mnemonic))
                    AssembleLine(instructionSyntax);
            }

            Instruction[] instructions = _instructionBytes.GetInstructionsFromBytes();

            return instructions;
        }

        private static void AddLabelToSymbolTable(string label)
        {
            _symbolTable[label] = (byte)_instructionBytes.Count;
        }

        private void AssembleLine(InstructionSyntax instructionSyntax)
        {
            switch (instructionSyntax.Mnemonic.ToLowerInvariant())
            {
                case "load":
                    Load(instructionSyntax.Operands);
                    break;
                case "store":
                    Store(instructionSyntax.Operands);
                    break;
                case "move":
                    Move(instructionSyntax.Operands);
                    break;
                case "addi":
                    Addi(instructionSyntax.Operands);
                    break;
                case "addf":
                    Addf(instructionSyntax.Operands);
                    break;
                case "jmpeq":
                    JmpEQ(instructionSyntax.Operands);
                    break;
                case "jmple":
                    JmpLE(instructionSyntax.Operands);
                    break;
                case "jmp":
                    Jmp(instructionSyntax.Operands);
                    break;
                case "and":
                    And(instructionSyntax.Operands);
                    break;
                case "or":
                    Or(instructionSyntax.Operands);
                    break;
                case "xor":
                    Xor(instructionSyntax.Operands);
                    break;
                case "ror":
                    Ror(instructionSyntax.Operands);
                    break;
                case "db":
                    DataByte(instructionSyntax.Operands);
                    break;
                case "org":
                    Org(instructionSyntax.Operands);
                    break;
                case "halt":
                    Halt(instructionSyntax.Operands);
                    break;
                default:
                    throw new UnrecognizedMnemonicException();
            }
        }

        private void DataByte(string[] operands)
        {
            bool invalidSyntax = false;

            if (operands.Length == 0)
                invalidSyntax = true;
            else
                foreach (string operand in operands)
                {
                    byte number;
                    string stringLiteral;

                    if (NumberSyntax.TryParseNumber(operand, out number))
                        _instructionBytes.Add(new InstructionByte(number));
                    else if (StringLiteralSyntax.TryParseStringLiteral(operand, out stringLiteral))
                        foreach (char c in stringLiteral)
                            _instructionBytes.Add(new InstructionByte((byte)c));
                    else
                    {
                        invalidSyntax = true;
                        break;
                    }
                }

            if (invalidSyntax)
                throw new AssemblyException("Expected a number or string literal.");
        }

        private void Org(string[] operands)
        {
            bool invalidSyntax = false;

            if (operands.Length != 1)
                invalidSyntax = true;
            else
            {
                byte number;
                if (NumberSyntax.TryParseNumber(operands[0], out number))
                    _instructionBytes.OriginAddress = number;
                else
                    invalidSyntax = true;
            }

            if (invalidSyntax)
                throw new AssemblyException("Expected a single number.");
        }

        private void Jmp(string[] operands)
        {
            bool invalidSyntax = false;

            if (operands.Length != 1)
                invalidSyntax = true;
            else
            {
                AddressSyntax address;
                if (AddressSyntax.TryParseAddress(operands[0], out address))
                {
                    _instructionBytes.Add(new InstructionByte(ByteUtilities.GetByteFromNibbles((byte)Opcode.JumpEqual, 0x0)));
                    _instructionBytes.Add(new InstructionByte(address));
                }
                else
                    invalidSyntax = true;
            }

            if (invalidSyntax)
                throw new AssemblyException("Expected a single address.");
        }

        private void JmpEQ(string[] operands)
        {
            RegisterSyntax register;
            AddressSyntax address;

            if (RegisterSyntax.TryParseRegister(operands[0].Split('=')[0], out register) && AddressSyntax.TryParseAddress(operands[1], out address))
            {
                _instructionBytes.Add(new InstructionByte(ByteUtilities.GetByteFromNibbles((byte)Opcode.JumpEqual, register.GetRegisterIndex())));
                _instructionBytes.Add(new InstructionByte(address));
            }
        }

        private void JmpLE(string[] operands)
        {
            RegisterSyntax register;
            AddressSyntax address;

            if (RegisterSyntax.TryParseRegister(operands[0].Split('<', '=')[0], out register) && AddressSyntax.TryParseAddress(operands[1], out address))
            {
                _instructionBytes.Add(new InstructionByte(ByteUtilities.GetByteFromNibbles((byte)Opcode.JumpLessEqual, register.GetRegisterIndex())));
                _instructionBytes.Add(new InstructionByte(address));
            }
        }

        private void Load(string[] operands)
        {
            RegisterSyntax register;
            RegisterSyntax register1;
            RegisterSyntax register2;

            AddressSyntax address;

            if (RegisterSyntax.TryParseRegister(operands[0], out register) && AddressSyntax.TryParseAddress(operands[1], out address))
            {
                _instructionBytes.Add(new InstructionByte(ByteUtilities.GetByteFromNibbles((byte)Opcode.ImmediateLoad, register.GetRegisterIndex())));
                _instructionBytes.Add(new InstructionByte(address));
            }
            else if (RegisterSyntax.TryParseRegister(operands[0], out register) && AddressSyntax.TryParseAddress(operands[1], out address, BracketExpectation.Present))
            {
                _instructionBytes.Add(new InstructionByte(ByteUtilities.GetByteFromNibbles((byte)Opcode.DirectLoad, register.GetRegisterIndex())));
                _instructionBytes.Add(new InstructionByte(address));
            }
            else if (RegisterSyntax.TryParseRegister(operands[0], out register1) && RegisterSyntax.TryParseRegister(operands[1], out register2, BracketExpectation.Present))
            {
                _instructionBytes.Add(new InstructionByte(ByteUtilities.GetByteFromNibbles((byte)Opcode.IndirectLoad, 0x0)));
                _instructionBytes.Add(new InstructionByte(ByteUtilities.GetByteFromNibbles(register1.GetRegisterIndex(), register2.GetRegisterIndex())));
            }
        }

        private void Store(string[] operands)
        {
            RegisterSyntax register;
            RegisterSyntax register1;
            RegisterSyntax register2;

            AddressSyntax address;

            if (RegisterSyntax.TryParseRegister(operands[0], out register) && AddressSyntax.TryParseAddress(operands[1], out address, BracketExpectation.Present))
            {
                _instructionBytes.Add(new InstructionByte(ByteUtilities.GetByteFromNibbles((byte)Opcode.DirectStore, register.GetRegisterIndex())));
                _instructionBytes.Add(new InstructionByte(address));
            }
            else if (RegisterSyntax.TryParseRegister(operands[0], out register1) && RegisterSyntax.TryParseRegister(operands[1], out register2, BracketExpectation.Present))
            {
                _instructionBytes.Add(new InstructionByte(ByteUtilities.GetByteFromNibbles((byte)Opcode.IndirectStore, 0x0)));
                _instructionBytes.Add(new InstructionByte(ByteUtilities.GetByteFromNibbles(register1.GetRegisterIndex(), register2.GetRegisterIndex())));
            }
        }

        private void Move(string[] operands)
        {
            RegisterSyntax register1;
            RegisterSyntax register2;

            if (RegisterSyntax.TryParseRegister(operands[0], out register1) && RegisterSyntax.TryParseRegister(operands[1], out register2))
            {
                _instructionBytes.Add(new InstructionByte(ByteUtilities.GetByteFromNibbles((byte)Opcode.Move, 0x0)));
                _instructionBytes.Add(new InstructionByte(ByteUtilities.GetByteFromNibbles(register1.GetRegisterIndex(), register2.GetRegisterIndex())));
            }
        }

        private void And(string[] operands)
        {
            RegisterSyntax register1;
            RegisterSyntax register2;
            RegisterSyntax register3;

            if (RegisterSyntax.TryParseRegister(operands[0], out register1) && RegisterSyntax.TryParseRegister(operands[1], out register2) && RegisterSyntax.TryParseRegister(operands[2], out register3))
            {
                _instructionBytes.Add(new InstructionByte(ByteUtilities.GetByteFromNibbles((byte)Opcode.And, register1.GetRegisterIndex())));
                _instructionBytes.Add(new InstructionByte(ByteUtilities.GetByteFromNibbles(register2.GetRegisterIndex(), register3.GetRegisterIndex())));
            }
        }

        private void Or(string[] operands)
        {
            RegisterSyntax register1;
            RegisterSyntax register2;
            RegisterSyntax register3;

            if (RegisterSyntax.TryParseRegister(operands[0], out register1) && RegisterSyntax.TryParseRegister(operands[1], out register2) && RegisterSyntax.TryParseRegister(operands[2], out register3))
            {
                _instructionBytes.Add(new InstructionByte(ByteUtilities.GetByteFromNibbles((byte)Opcode.Or, register1.GetRegisterIndex())));
                _instructionBytes.Add(new InstructionByte(ByteUtilities.GetByteFromNibbles(register2.GetRegisterIndex(), register3.GetRegisterIndex())));
            }
        }

        private void Xor(string[] operands)
        {
            RegisterSyntax register1;
            RegisterSyntax register2;
            RegisterSyntax register3;

            if (RegisterSyntax.TryParseRegister(operands[0], out register1) && RegisterSyntax.TryParseRegister(operands[1], out register2) && RegisterSyntax.TryParseRegister(operands[2], out register3))
            {
                _instructionBytes.Add(new InstructionByte(ByteUtilities.GetByteFromNibbles((byte)Opcode.Xor, register1.GetRegisterIndex())));
                _instructionBytes.Add(new InstructionByte(ByteUtilities.GetByteFromNibbles(register2.GetRegisterIndex(), register3.GetRegisterIndex())));
            }
        }

        private void Ror(string[] operands)
        {
            RegisterSyntax register;
            byte number;

            if (RegisterSyntax.TryParseRegister(operands[0], out register) && NumberSyntax.TryParseNumber(operands[1], out number))
                if (number < 16)
                {
                    _instructionBytes.Add(new InstructionByte(ByteUtilities.GetByteFromNibbles((byte)Opcode.Ror, register.GetRegisterIndex())));
                    _instructionBytes.Add(new InstructionByte(ByteUtilities.GetByteFromNibbles(0x0, number)));
                }
                else
                    throw new AssemblyException("Number cannot be larger than 15.");
        }

        private void Addi(string[] operands)
        {
            RegisterSyntax register1;
            RegisterSyntax register2;
            RegisterSyntax register3;

            if (RegisterSyntax.TryParseRegister(operands[0], out register1) && RegisterSyntax.TryParseRegister(operands[1], out register2) && RegisterSyntax.TryParseRegister(operands[2], out register3))
            {
                _instructionBytes.Add(new InstructionByte(ByteUtilities.GetByteFromNibbles((byte)Opcode.IntegerAdd, register1.GetRegisterIndex())));
                _instructionBytes.Add(new InstructionByte(ByteUtilities.GetByteFromNibbles(register2.GetRegisterIndex(), register3.GetRegisterIndex())));
            }
        }

        private void Addf(string[] operands)
        {
            RegisterSyntax register1;
            RegisterSyntax register2;
            RegisterSyntax register3;

            if (RegisterSyntax.TryParseRegister(operands[0], out register1) && RegisterSyntax.TryParseRegister(operands[1], out register2) && RegisterSyntax.TryParseRegister(operands[2], out register3))
            {
                _instructionBytes.Add(new InstructionByte(ByteUtilities.GetByteFromNibbles((byte)Opcode.FloatingPointAdd, register1.GetRegisterIndex())));
                _instructionBytes.Add(new InstructionByte(ByteUtilities.GetByteFromNibbles(register2.GetRegisterIndex(), register3.GetRegisterIndex())));
            }
        }

        private void Halt(string[] operands)
        {
            _instructionBytes.Add(new InstructionByte(ByteUtilities.GetByteFromNibbles((byte)Opcode.Halt, 0x0)));
            _instructionBytes.Add(new InstructionByte(0x00));
        }

        private class InstructionSyntax
        {
            public string Comment { get; }
            public string Label { get; }
            public string Mnemonic { get; }
            public string[] Operands { get; }

            private const char CommentDelimiter = ';';
            private const char LabelDelimiter = ':';

            private InstructionSyntax(string comment, string label, string mnemonic, string[] operands)
            {
                Comment = comment;
                Label = label;
                Mnemonic = mnemonic;
                Operands = operands;
            }

            public static InstructionSyntax Parse(string line)
            {
                string comment = GetComment(ref line);

                string label = GetLabel(ref line);

                string mnemonic = GetMnemonic(line);

                string[] operands = GetOperands(line);

                return new InstructionSyntax(comment, label, mnemonic, operands);
            }

            private static string GetComment(ref string line)
            {
                string comment = "";

                string[] split = line.Split(new[] { CommentDelimiter }, 2);

                if (split.Length == 2)
                {
                    line = split[0];
                    comment = split[1].Trim();
                }

                return comment;
            }

            private static string GetLabel(ref string line)
            {
                string label = "";

                int delimiterIndex = line.IndexOf(LabelDelimiter);

                if (delimiterIndex > -1)
                {
                    label = line.Substring(0, delimiterIndex + 1).Trim();
                    line = line.Substring(delimiterIndex + 1);

                    if (!IsValidLabel(label))
                        throw new LabelAssemblyException();

                    label = label.TrimEnd(LabelDelimiter);
                }

                return label;
            }

            private static bool IsValidLabel(string input)
            {
                if (input.Length == 1 && input[0] == LabelDelimiter)
                    return false;

                if (input.Last() != LabelDelimiter)
                    return false;

                foreach (char c in input.TrimEnd(LabelDelimiter))
                    if (!"ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789#_~".Contains(c))
                        return false;

                if (char.IsNumber(input[0]))
                    return false;

                return true;
            }

            private static string GetMnemonic(string line)
            {
                string[] split = line.Trim().Split();

                string mnemonic = split[0];

                return mnemonic;
            }

            private static string[] GetOperands(string line)
            {
                string[] split = line.Trim().Split(null, 2);

                string[] operands;

                if (split.Length == 2)
                    operands = split[1].Split(',').Select(o => o.Trim()).ToArray();
                else
                    operands = new string[] { };

                return operands;
            }

            public override string ToString()
            {
                return $"{Mnemonic} {string.Join(",", Operands)}";
            }
        }

        private class RegisterSyntax
        {
            private string Value { get; }

            private RegisterSyntax(string value)
            {
                Value = value;
            }

            public byte GetRegisterIndex()
            {
                return ByteUtilities.ConvertHexStringToByte(Value[1].ToString());
            }

            public static bool TryParseRegister(string input, out RegisterSyntax registerSyntax, BracketExpectation bracketExpectation = BracketExpectation.NotPresent)
            {
                bool isSuccess;

                bool isRegister = Regex.IsMatch(input, @"^\[?R[0-9A-F]\]?$");
                bool isSurroundedByBrackets = input.StartsWith("[") && input.EndsWith("]");

                if (bracketExpectation == BracketExpectation.Present)
                    isSuccess = isRegister && isSurroundedByBrackets;
                else
                    isSuccess = isRegister && !isSurroundedByBrackets;

                if (isSuccess)
                    registerSyntax = new RegisterSyntax(input.Trim('[', ']'));
                else
                    registerSyntax = null;

                return isSuccess;
            }

            public override string ToString()
            {
                return Value;
            }
        }

        private static class NumberSyntax
        {
            public static bool TryParseNumber(string input, out byte number)
            {
                return TryParseDecimalLiteral(input, out number) || TryParseBinaryLiteral(input, out number) || TryParseHexLiteral(input, out number);
            }

            private static bool TryParseDecimalLiteral(string input, out byte number)
            {
                sbyte signedNumber;

                bool success = sbyte.TryParse(input.TrimEnd('d'), out signedNumber);

                if (signedNumber < 0)
                    number = (byte)signedNumber;
                else
                    success = byte.TryParse(input.TrimEnd('d'), out number);

                return success;
            }

            private static bool TryParseBinaryLiteral(string input, out byte number)
            {
                bool success;

                try
                {
                    number = Convert.ToByte(input.TrimEnd('b'), 2);
                    success = true;
                }
                catch
                {
                    number = 0;
                    success = false;
                }

                return success;
            }

            private static bool TryParseHexLiteral(string input, out byte number)
            {
                bool success = byte.TryParse(input.TrimStart("0x".ToCharArray()), NumberStyles.HexNumber, null, out number)
                               || byte.TryParse(input.TrimStart('$'), NumberStyles.HexNumber, null, out number)
                               || (byte.TryParse(input.TrimEnd('h'), NumberStyles.HexNumber, null, out number) && !char.IsLetter(input.FirstOrDefault()));

                if (!success)
                    number = 0;

                return success;
            }
        }

        private class AddressSyntax
        {
            public byte Value { get; }

            public string UndefinedLabel { get; }

            public bool ContainsUndefinedLabel => !string.IsNullOrWhiteSpace(UndefinedLabel);

            private AddressSyntax(byte value, string undefinedLabel)
            {
                Value = value;
                UndefinedLabel = undefinedLabel;
            }

            public static bool TryParseAddress(string input, out AddressSyntax addressSyntax, BracketExpectation bracketExpectation = BracketExpectation.NotPresent)
            {
                bool isSuccess;

                byte address;
                string undefinedLabel;
                bool isAddress = IsAddress(input.Trim('[', ']'), out address, out undefinedLabel);

                bool isSurroundedByBrackets = input.StartsWith("[") && input.EndsWith("]");

                if (bracketExpectation == BracketExpectation.Present)
                    isSuccess = isAddress && isSurroundedByBrackets;
                else
                    isSuccess = isAddress && !isSurroundedByBrackets;

                if (isSuccess)
                    addressSyntax = new AddressSyntax(address, undefinedLabel);
                else
                    addressSyntax = null;

                return isSuccess;
            }

            private static bool IsAddress(string input, out byte address, out string undefinedLabel)
            {
                undefinedLabel = null;

                if (NumberSyntax.TryParseNumber(input, out address))
                    return true;

                if (IsRegister(input))
                    return false;

                if (_symbolTable.ContainsKey(input))
                    address = _symbolTable[input];
                else
                    undefinedLabel = input;

                return true;
            }

            private static bool IsRegister(string input)
            {
                RegisterSyntax register;
                return RegisterSyntax.TryParseRegister(input, out register);
            }

            public override string ToString()
            {
                return Value.ToString();
            }
        }

        private static class StringLiteralSyntax
        {
            public static bool TryParseStringLiteral(string input, out string stringLiteral)
            {
                const char doubleQuote = '"';
                const char singleQuote = '\'';

                if (input.First() == doubleQuote && input.Last() == doubleQuote)
                {
                    stringLiteral = input.TrimStart(doubleQuote).TrimEnd(doubleQuote);
                    return true;
                }
                else if (input.First() == singleQuote && input.Last() == singleQuote)
                {
                    stringLiteral = input.TrimStart(singleQuote).TrimEnd(singleQuote);
                    return true;
                }
                else
                {
                    stringLiteral = null;
                    return false;
                }
            }
        }

        private class InstructionByteCollection
        {
            private readonly InstructionByte[] _bytes;
            private byte _originAddress;

            public InstructionByteCollection()
            {
                _bytes = new InstructionByte[0x100];
            }

            public byte OriginAddress
            {
                private get
                {
                    return _originAddress;
                }
                set
                {
                    _originAddress = value;

                    if (_originAddress > Count)
                        Count = _originAddress;
                }
            }

            public int Count { get; private set; }

            public void Add(InstructionByte instructionByte)
            {
                _bytes[OriginAddress] = instructionByte;
                OriginAddress++;
            }

            public void Reset()
            {
                Array.Clear(_bytes, 0, _bytes.Length);
                OriginAddress = 0;
                Count = 0;
            }

            public Instruction[] GetInstructionsFromBytes()
            {
                IList<Instruction> instructions = new List<Instruction>();

                for (int i = 0; i < Count; i += 2)
                {
                    byte byte1 = 0x00;
                    byte byte2 = 0x00;

                    if (_bytes[i] != null)
                        byte1 = _bytes[i].GetValue();

                    if (_bytes[i + 1] != null)
                        byte2 = _bytes[i + 1].GetValue();

                    instructions.Add(new Instruction(byte1, byte2));
                }

                return instructions.ToArray();
            }
        }

        private class InstructionByte
        {
            private readonly byte _byte;
            private readonly AddressSyntax _address;
            private readonly AddressType _addressType;

            public InstructionByte(byte @byte)
            {
                _byte = @byte;
                _addressType = AddressType.DirectValue;
            }

            public InstructionByte(AddressSyntax address)
            {
                _address = address;
                if (address.ContainsUndefinedLabel)
                    _addressType = AddressType.Label;
                else
                {
                    _addressType = AddressType.DirectValue;
                    _byte = _address.Value;
                }
            }

            public byte GetValue()
            {
                byte value = 0x00;

                switch (_addressType)
                {
                    case AddressType.DirectValue:
                        value = _byte;
                        break;
                    case AddressType.Label:
                        value = _symbolTable[_address.UndefinedLabel];
                        break;
                }

                return value;
            }

            private enum AddressType
            {
                DirectValue,
                Label
            }
        }

        private enum BracketExpectation
        {
            Present,
            NotPresent
        }
    }
}