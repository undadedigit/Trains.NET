﻿using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows;
using Comet;
using Trains.NET.Engine;
using Trains.NET.Rendering;

namespace Trains.NET.Comet
{
    public class MainPage : View
    {
        private readonly State<bool> _configurationShown = false;

        private readonly Timer _timer;
        private readonly IGameBoard _gameBoard;
        private readonly MiniMapDelegate _miniMapDelegate;
        private Size _lastSize = Size.Empty;

        public MainPage(IGame game,
                        IPixelMapper pixelMapper,
                        OrderedList<ITool> tools,
                        OrderedList<ILayerRenderer> layers,
                        OrderedList<ICommand> commands,
                        ITrainController trainControls,
                        IGameBoard gameBoard,
                        ITrackParameters trackParameters)
        {
            this.Title("Trains - " + ThisAssembly.AssemblyInformationalVersion);

            var controlDelegate = new TrainsDelegate(game, pixelMapper);
            _miniMapDelegate = new MiniMapDelegate(gameBoard, trackParameters, pixelMapper);

            this.Body = () =>
            {
                return new HStack()
                {
                    new VStack()
                    {
                        new ToggleButton("Configuration", _configurationShown, ()=> _configurationShown.Value = !_configurationShown.Value),
                        new Spacer(),
                        _configurationShown ?
                                CreateConfigurationControls(layers) :
                                CreateToolsControls(tools, controlDelegate),
                        new Spacer(),
                        _configurationShown ? null :
                            CreateCommandControls(commands),
                        new Spacer(),
                        new DrawableControl(_miniMapDelegate).Frame(height: 100)
                    }.Frame(100, alignment: Alignment.Top),
                    new VStack()
                    {
                        new TrainControllerPanel(trainControls),
                        new DrawableControl(controlDelegate)
                    }
                }.FillHorizontal();
            };

            _timer = new Timer((state) =>
            {
                game.AdjustViewPortIfNecessary();

                ThreadHelper.Run(async () =>
                {
                    await ThreadHelper.SwitchToMainThreadAsync();

                    controlDelegate.Invalidate();
                    _miniMapDelegate.Invalidate();
                });
            }, null, 0, 16);
            _gameBoard = gameBoard;
        }

        public void Save()
        {
            _gameBoard.SaveTracks();
        }

        public void Redraw(Size newSize)
        {
            if (Math.Abs(newSize.Width - _lastSize.Width) >= 20 ||
                Math.Abs(newSize.Height - _lastSize.Height) >= 20)
            {
                _lastSize = newSize;
                ViewPropertyChanged(ResetPropertyString, null);
            }
        }

        private static View CreateCommandControls(IEnumerable<ICommand> commands)
        {
            var controlsGroup = new VStack();
            foreach (ICommand cmd in commands)
            {
                controlsGroup.Add(new Button(cmd.Name, () => cmd.Execute()));
            }

            return controlsGroup;
        }

        private static View CreateToolsControls(IEnumerable<ITool> tools, TrainsDelegate controlDelegate)
        {
            var controlsGroup = new RadioGroup(Orientation.Vertical);
            foreach (ITool tool in tools)
            {
                if (controlDelegate.CurrentTool.Value == null)
                {
                    controlDelegate.CurrentTool.Value = tool;
                }

                controlsGroup.Add(new RadioButton(() => tool.Name, () => controlDelegate.CurrentTool.Value == tool, () => controlDelegate.CurrentTool.Value = tool));
            }

            return controlsGroup;
        }

        private static View CreateConfigurationControls(IEnumerable<ILayerRenderer> layers)
        {
            var layersGroup = new VStack();
            foreach (ILayerRenderer layer in layers)
            {
                layersGroup.Add(new ToggleButton(layer.Name, layer.Enabled, () => layer.Enabled = !layer.Enabled));
            }
            return layersGroup;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _timer.Dispose();
                _miniMapDelegate.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
