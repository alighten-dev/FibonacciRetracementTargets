//
// Copyright (C) 2024, NinjaTrader LLC <www.ninjatrader.com>.
// NinjaTrader reserves the right to modify or overwrite this NinjaScript component with each release.
//
#region Using declarations
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Cbi;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.SuperDom;
using NinjaTrader.Data;
using NinjaTrader.NinjaScript;
using NinjaTrader.Core.FloatingPoint;
using NinjaTrader.NinjaScript.DrawingTools;
#endregion

//This namespace holds Indicators in this folder and is required. Do not change it.
namespace NinjaTrader.NinjaScript.Indicators
{
	public class FibRetracementTargets : Indicator
	{
		private NinjaTrader.Gui.Tools.SimpleFont myFont;
		private Swing Swing1;
		private int trendHighBarsAgo;
		private int trendLowBarsAgo;
		private double trendSwingHigh;
		private double trendSwingLow;
		private int swingHighBarsAgo;
		private int swingLowBarsAgo;
		private double latestSwingHigh;
		private double latestSwingLow;
		private int latestSwingHighBar;
		private int latestSwingLowBar;
		private int previousSwingHighBar;
		private int previousSwingLowBar;
		private double bearFibLevel1;
		private double bearFibLevel2;
		private double bullFibLevel1;
		private double bullFibLevel2;
		
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Calculate					= Calculate.OnBarClose;
				Description					= @"Automatically draws Fibonacci Retracement target boxes. Based on the Swing indicator with higher-highs and lower-lows confirmation.";
				Name						= "Fibonacci Retracement Targets";
				DrawOnPricePanel			= true;
				IsOverlay					= true;
				IsSuspendedWhileInactive	= true;

				SwingStrength = 10;
				MinSwingLength = 40;
				LowFibPercent = 50.0;
				HighFibPercent = 76.4;
				RequireSwingTrend = true;
				
				myFont = new NinjaTrader.Gui.Tools.SimpleFont("Courier New", 12) { Size = 25, Bold = true };
				previousSwingHighBar = 0;
				previousSwingLowBar = 0;
				
			
			}
			else if (State == State.Historical)
			{
				if (Calculate == Calculate.OnPriceChange)
				{
					
				}
			}
			else if (State == State.DataLoaded)
			{
				ClearOutputWindow();
				
				Swing1 = Swing(SwingStrength);
			}
		}

		protected override void OnBarUpdate()
		{
			if (CurrentBar < SwingStrength)
                return;
			
			
			if(IsFirstTickOfBar)
			{
				//Use these swings to confirm that we are trending in a direction
				trendHighBarsAgo = Swing1.SwingHighBar(0, 2, Bars.BarsSinceNewTradingDay);
				trendLowBarsAgo = Swing1.SwingLowBar(0, 2, Bars.BarsSinceNewTradingDay);
				
				//Use these swings to calculate the Fibonacci Retracement
				swingHighBarsAgo = Swing1.SwingHighBar(0, 1, Bars.BarsSinceNewTradingDay);
				swingLowBarsAgo = Swing1.SwingLowBar(0, 1, Bars.BarsSinceNewTradingDay);
				latestSwingHighBar = CurrentBar - swingHighBarsAgo;
				latestSwingLowBar = CurrentBar - swingLowBarsAgo;
				
				
				if(trendHighBarsAgo > -1 && trendLowBarsAgo > -1)
				{
					
					try
					{			
					
						trendSwingHigh = Swing1.SwingHigh[trendHighBarsAgo];
						trendSwingLow =  Swing1.SwingLow[trendLowBarsAgo];
						
						latestSwingHigh = Swing1.SwingHigh[swingHighBarsAgo];
						latestSwingLow =  Swing1.SwingLow[swingLowBarsAgo];
						
					}
					catch (Exception e)
					{
						// In case the indicator has already been Terminated, you can safely ignore errors
						if (State >= State.Terminated)
							return;
						
						Log("FibRetracementTargets Error: Please check your indicator for errors.", NinjaTrader.Cbi.LogLevel.Warning);
						
						Print(Time[0] + " " + e.ToString());
					}
					
					
					
					//Bear Fibonocci Retracement
					if (swingHighBarsAgo > swingLowBarsAgo && (latestSwingHigh - latestSwingLow >= MinSwingLength) && latestSwingHighBar != previousSwingHighBar)
					{
						if(RequireSwingTrend)
						{
							if(latestSwingLow > trendSwingLow)
								return;
						}
						bearFibLevel1 = ((High[swingHighBarsAgo] - Low[swingLowBarsAgo]) *LowFibPercent/100) + Low[swingLowBarsAgo];
						bearFibLevel2 = ((High[swingHighBarsAgo] - Low[swingLowBarsAgo]) *HighFibPercent/100) + Low[swingLowBarsAgo];
			
						Draw.Rectangle(this, CurrentBar + "-FibBox", true, swingLowBarsAgo, bearFibLevel1, -50, bearFibLevel2, Brushes.Crimson, Brushes.Crimson, 0);
											
						previousSwingHighBar = latestSwingHighBar;
						
					}
					
					
					//Bull Fibonocci Retracement
					if (swingLowBarsAgo > swingHighBarsAgo && (latestSwingHigh - latestSwingLow >= MinSwingLength) && latestSwingHighBar != previousSwingHighBar)
					{
						if(RequireSwingTrend)
						{
							if(latestSwingHigh < trendSwingHigh)
								return;
						}
						
						bullFibLevel1 = High[swingHighBarsAgo] - ((High[swingHighBarsAgo] - Low[swingLowBarsAgo]) *LowFibPercent/100);
						bullFibLevel2 = High[swingHighBarsAgo] - ((High[swingHighBarsAgo] - Low[swingLowBarsAgo]) *HighFibPercent/100);
						
						Draw.Rectangle(this, CurrentBar + "-FibBox", true, swingHighBarsAgo, bullFibLevel1, -50, bullFibLevel2, Brushes.LimeGreen, Brushes.LimeGreen, 0);	
						
						previousSwingHighBar = latestSwingHighBar;
					}
						
					
				}
				
			}
			
		}

		#region Properties
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="SwingStrength", Order=1, GroupName="Parameters")]
		public int SwingStrength
		{ get; set; }
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="MinSwingLength", Order=2, GroupName="Parameters")]
		public int MinSwingLength
		{ get; set; }
		[NinjaScriptProperty]
		[Range(1, double.MaxValue)]
		[Display(Name="LowFibPercent", Order=3, GroupName="Parameters")]
		public double LowFibPercent
		{ get; set; }
		[NinjaScriptProperty]
		[Range(1, double.MaxValue)]
		[Display(Name="HighFibPercent", Order=4, GroupName="Parameters")]
		public double HighFibPercent
		{ get; set; }
		[NinjaScriptProperty]
		[Display(Name="RequireSwingTrend", Order=5, GroupName="Parameters")]
		public bool RequireSwingTrend
		{ get; set; }
		
		
		#endregion
	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private FibRetracementTargets[] cacheFibRetracementTargets;
		public FibRetracementTargets FibRetracementTargets(int swingStrength, int minSwingLength, double lowFibPercent, double highFibPercent, bool requireSwingTrend)
		{
			return FibRetracementTargets(Input, swingStrength, minSwingLength, lowFibPercent, highFibPercent, requireSwingTrend);
		}

		public FibRetracementTargets FibRetracementTargets(ISeries<double> input, int swingStrength, int minSwingLength, double lowFibPercent, double highFibPercent, bool requireSwingTrend)
		{
			if (cacheFibRetracementTargets != null)
				for (int idx = 0; idx < cacheFibRetracementTargets.Length; idx++)
					if (cacheFibRetracementTargets[idx] != null && cacheFibRetracementTargets[idx].SwingStrength == swingStrength && cacheFibRetracementTargets[idx].MinSwingLength == minSwingLength && cacheFibRetracementTargets[idx].LowFibPercent == lowFibPercent && cacheFibRetracementTargets[idx].HighFibPercent == highFibPercent && cacheFibRetracementTargets[idx].RequireSwingTrend == requireSwingTrend && cacheFibRetracementTargets[idx].EqualsInput(input))
						return cacheFibRetracementTargets[idx];
			return CacheIndicator<FibRetracementTargets>(new FibRetracementTargets(){ SwingStrength = swingStrength, MinSwingLength = minSwingLength, LowFibPercent = lowFibPercent, HighFibPercent = highFibPercent, RequireSwingTrend = requireSwingTrend }, input, ref cacheFibRetracementTargets);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.FibRetracementTargets FibRetracementTargets(int swingStrength, int minSwingLength, double lowFibPercent, double highFibPercent, bool requireSwingTrend)
		{
			return indicator.FibRetracementTargets(Input, swingStrength, minSwingLength, lowFibPercent, highFibPercent, requireSwingTrend);
		}

		public Indicators.FibRetracementTargets FibRetracementTargets(ISeries<double> input , int swingStrength, int minSwingLength, double lowFibPercent, double highFibPercent, bool requireSwingTrend)
		{
			return indicator.FibRetracementTargets(input, swingStrength, minSwingLength, lowFibPercent, highFibPercent, requireSwingTrend);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.FibRetracementTargets FibRetracementTargets(int swingStrength, int minSwingLength, double lowFibPercent, double highFibPercent, bool requireSwingTrend)
		{
			return indicator.FibRetracementTargets(Input, swingStrength, minSwingLength, lowFibPercent, highFibPercent, requireSwingTrend);
		}

		public Indicators.FibRetracementTargets FibRetracementTargets(ISeries<double> input , int swingStrength, int minSwingLength, double lowFibPercent, double highFibPercent, bool requireSwingTrend)
		{
			return indicator.FibRetracementTargets(input, swingStrength, minSwingLength, lowFibPercent, highFibPercent, requireSwingTrend);
		}
	}
}

#endregion
