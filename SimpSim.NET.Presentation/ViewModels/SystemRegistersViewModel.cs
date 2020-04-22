﻿using System.Windows.Input;

namespace SimpSim.NET.Presentation.ViewModels
{
    public class SystemRegistersViewModel : ViewModelBase
    {
        private readonly SimpleSimulator _simulator;

        public SystemRegistersViewModel(SimpleSimulator simulator)
        {
            _simulator = simulator;

            ResetProgramCounterCommand = new Command(() => _simulator.Machine.ProgramCounter = 0x00, () => true, _simulator);

            _simulator.Machine.ProgramCounterChanged += () => { OnPropertyChanged("ProgramCounter"); };
            _simulator.Machine.InstructionRegisterChanged += () => { OnPropertyChanged("InstructionRegister"); };
        }

        public ICommand ResetProgramCounterCommand { get; }

        public byte ProgramCounter
        {
            get => _simulator.Machine.ProgramCounter;
            set
            {
                _simulator.Machine.ProgramCounter = value;
                OnPropertyChanged("ProgramCounter");
            }
        }

        public Instruction InstructionRegister => _simulator.Machine.InstructionRegister;
    }
}
