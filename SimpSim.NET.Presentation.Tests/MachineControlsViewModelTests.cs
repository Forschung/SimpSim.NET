﻿using NUnit.Framework;
using SimpSim.NET.Presentation.ViewModels;

namespace SimpSim.NET.Presentation.Tests
{
    [TestFixture]
    public class MachineControlsViewModelTests
    {
        private SimpleSimulator _simulator;
        private MachineControlsViewModel _viewModel;

        [SetUp]
        public void SetUp()
        {
            _simulator = new SimpleSimulator();
            _viewModel = new MachineControlsViewModel(_simulator);
        }

        [Test]
        public void ClearMemoryCommandShouldClearMemory()
        {
            for (int i = 0; i <= byte.MaxValue; i++)
                _simulator.Memory[(byte)i] = 0xFF;

            _viewModel.ClearMemoryCommand.Execute(null);

            for (int i = 0; i <= byte.MaxValue; i++)
                Assert.AreEqual(0x00, _simulator.Memory[(byte)i]);
        }

        [Test]
        public void ClearRegistersCommandShouldClearRegisters()
        {
            for (byte b = 0; b <= 0x0F; b++)
                _simulator.Registers[b] = 0xFF;

            _viewModel.ClearRegistersCommand.Execute(null);

            for (byte b = 0; b <= 0x0F; b++)
                Assert.AreEqual(0x00, _simulator.Registers[b]);
        }
    }
}
