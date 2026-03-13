using System;

namespace NewBootMode
{
    public class FileTypeException : Exception
    {
        public FileTypeException() : base($"Invalid file type") { }
        public FileTypeException(string type) : base($"Invalid file type {type}") { }
    }

    public class BootParseException : Exception
    {
        public BootParseException(Exception innerException) : base($"Boot error: {innerException.Message}") {}
    }

    public class InvalidOpCodeException : Exception
    {
        public InvalidOpCodeException(byte opcode) : base($"Invalid opcode: {opcode:X4}") {}
    }
}