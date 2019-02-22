﻿using System;
using System.Windows;
using EPBLib;
using EPBLib.Logic;

namespace EPBLab.ViewModel.Logic
{
    public class SignalOperatorViewModel : LogicNodeViewModel
    {
        protected EpBlueprint Blueprint;
        public EpbSignalOperator Operator { get; protected set; }

        public string Name => Operator.OutSig;
        public string OpName => Operator.OpName;
        public string OutSig => Operator.OutSig;


        public SignalOperatorViewModel(EpBlueprint blueprint, EpbSignalOperator op, Point initialPosition)
        {
            Blueprint = blueprint;
            Operator = op;
            X = initialPosition.X;
            Y = initialPosition.Y;

            switch (op)
            {
                case EpbSignalOperatorAnd2 opAnd2:
                    NodeType = "AND";
                    AddInputs(2);                  
                    break;

                case EpbSignalOperatorAnd4 opAnd4:
                    NodeType = "AND";
                    AddInputs(4);
                    break;

                case EpbSignalOperatorNand2 opNand2:
                    NodeType = "NAND";
                    AddInputs(2);
                    break;

                case EpbSignalOperatorNand4 opNand4:
                    NodeType = "NAND";
                    AddInputs(4);
                    break;

                case EpbSignalOperatorOr2 opOr2:
                    NodeType = "OR";
                    AddInputs(2);
                    break;

                case EpbSignalOperatorOr4 opOr4:
                    NodeType = "OR";
                    AddInputs(4);
                    break;

                case EpbSignalOperatorNor2 opNor2:
                    NodeType = "NOR";
                    AddInputs(2);
                    break;

                case EpbSignalOperatorNor4 opNor4:
                    NodeType = "NOR";
                    AddInputs(4);
                    break;

                case EpbSignalOperatorXor opXor:
                    NodeType = "XOR";
                    AddInputs(2);
                    break;

                case EpbSignalOperatorXnor opXnor:
                    NodeType = "XNOR";
                    AddInputs(2);
                    break;

                case EpbSignalOperatorInverter opInverter:
                    NodeType = "NOT";
                    AddInputs(1);
                    break;

                case EpbSignalOperatorSRLatch opSrLatch:
                    NodeType = "Set/Reset Latch";
                    AddInputs(2);
                    break;

                case EpbSignalOperatorDelay opDelay:
                    NodeType = "Delay";
                    AddInputs(1);
                    break;

                default:
                    NodeType = "Unknown operator";
                    break;
            }

            Outputs.Add(new ConnectionPointViewModel()
            {
                Name = "0",
                Type = ConnectionPointViewModel.ConnectorType.OutputLast
            });
        }

        protected void AddInputs(int n)
        {
            for (int i = 0; i < n; i++)
            {
                Inputs.Add(new ConnectionPointViewModel()
                {
                    Name = $"{i}",
                    Type = i < n -1 ? ConnectionPointViewModel.ConnectorType.Input : ConnectionPointViewModel.ConnectorType.InputLast
                });

            }
        }

    }
}
