﻿using System.IO;
using Microsoft.Win32;
using SimpSim.NET.Presentation;

namespace SimpSim.NET.WPF
{
    internal class UserInputService : IUserInputService
    {
        public FileInfo GetOpenFileName()
        {
            return GetFileFromDialog(new OpenFileDialog());
        }

        public FileInfo GetSaveFileName()
        {
            return GetFileFromDialog(new SaveFileDialog());
        }

        private FileInfo GetFileFromDialog(FileDialog fileDialog)
        {
            fileDialog.ShowDialog();

            if (string.IsNullOrWhiteSpace(fileDialog.FileName))
                return null;
            else
                return new FileInfo(fileDialog.FileName);
        }
    }
}