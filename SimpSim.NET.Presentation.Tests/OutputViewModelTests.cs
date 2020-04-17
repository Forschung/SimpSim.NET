﻿using SimpSim.NET.Presentation.ViewModels;
using Xunit;

namespace SimpSim.NET.Presentation.Tests
{
    public class OutputViewModelTests
    {
        [Fact]
        public void ClearCommandShouldEmptyOutputWindow()
        {
            OutputViewModel viewModel = new OutputViewModel(new SimpleSimulator());

            viewModel.OutputWindowText = "This is some output text.";

            viewModel.ClearCommand.Execute(null);

            Assert.Null(viewModel.OutputWindowText);
        }
    }
}
