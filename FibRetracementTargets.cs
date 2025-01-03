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
		private Swing Swing2;
		private int trendHighBarsAgo;
		private int trendLowBarsAgo;
		private double trendSwingHigh;
		private double trendSwingLow;
		
		private int predictiveHighBarsAgo;
		private int predictiveLowBarsAgo;
		private double predictiveSwingHigh;
		private double predictiveSwingLow;
		
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
		
		private Series<bool> bullHasRetrace;
		private Series<int> bullRetraceStartBarsAgo;
		private Series<double> bullRetraceFibLevel1;
		private Series<double> bullRetraceFibLevel2;
		
		private Series<bool> bearHasRetrace;
		private Series<int> bearRetraceStartBarsAgo;
		private Series<double> bearRetraceFibLevel1;
		private Series<double> bearRetraceFibLevel2;
		
		private int previousPredictiveLowBarsAgo;
		private int previousPredictiveHighBarsAgo;
		private Rectangle predictiveBearRectangle;
		private int latestPredictiveSwingHighBar;
		private int latestPredictiveSwingLowBar;
		private int previousPredictiveSwingHighBar;
		private int previousPredictiveSwingLowBar;
		private string predictiveBearRectangleTag = "predictiveBearRectangle";
		private Rectangle predictiveBullRectangle;
		private string predictiveBullRectangleTag = "predictiveBullRectangle";
		
		private bool bearFibDetected;
		private bool bullFibDetected;
		

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

				SwingStrength = 15;
				PredictiveSwingStrength = 3;
				MinSwingLength = 40;
				LowFibPercent = 50.0;
				HighFibPercent = 76.4;
				RequireSwingTrend = true;
				UsePredictiveRetracements = true;
				FibTargetWidth = 50;
				
				myFont = new NinjaTrader.Gui.Tools.SimpleFont("Courier New", 12) { Size = 25, Bold = true };
				previousSwingHighBar = 0;
				previousSwingLowBar = 0;
				
			
			}
			else if (State == State.Historical)
			{
				
			}
			else if (State == State.DataLoaded)
			{
				ClearOutputWindow();
				
				Swing1 = Swing(SwingStrength);
				Swing2 = Swing(PredictiveSwingStrength);
								
				bullHasRetrace = new Series<bool>(this);
				bullRetraceStartBarsAgo = new Series<int>(this);
				bullRetraceFibLevel1 = new Series<double>(this);
				bullRetraceFibLevel2 = new Series<double>(this);
				
				bearHasRetrace = new Series<bool>(this);
				bearRetraceStartBarsAgo = new Series<int>(this);
				bearRetraceFibLevel1 = new Series<double>(this);
				bearRetraceFibLevel2 = new Series<double>(this);
			}
		}

		protected override void OnBarUpdate()
		{
			if (CurrentBar < SwingStrength)
                return;
			
			
			bearHasRetrace[0] = false;
			bearRetraceStartBarsAgo[0] = 0;
			bearRetraceFibLevel1[0] = 0;
			bearRetraceFibLevel2[0] = 0;
			bullHasRetrace[0] = false;
			bullRetraceStartBarsAgo[0] = 0;
			bullRetraceFibLevel1[0] = 0;
			bullRetraceFibLevel2[0] = 0;
			
			if(IsFirstTickOfBar)
			{
				//Use these swings to confirm that we are trending in a direction
				trendHighBarsAgo = Swing1.SwingHighBar(0, 2, Bars.BarsSinceNewTradingDay);
				trendLowBarsAgo = Swing1.SwingLowBar(0, 2, Bars.BarsSinceNewTradingDay);
				
				//Use these swings to temporarily calculate the Fibonacci Retracement
				predictiveHighBarsAgo = Swing2.SwingHighBar(0, 1, Bars.BarsSinceNewTradingDay);
				predictiveLowBarsAgo = Swing2.SwingLowBar(0, 1, Bars.BarsSinceNewTradingDay);
				latestPredictiveSwingHighBar = CurrentBar - predictiveHighBarsAgo;
				latestPredictiveSwingLowBar = CurrentBar - predictiveLowBarsAgo;
				
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
						
						predictiveSwingHigh = Swing2.SwingHigh[predictiveHighBarsAgo];
						predictiveSwingLow =  Swing2.SwingLow[predictiveLowBarsAgo];
						
						latestSwingHigh = Swing1.SwingHigh[swingHighBarsAgo];
						latestSwingLow =  Swing1.SwingLow[swingLowBarsAgo];
						
					}
					catch (Exception e)
					{
						// In case the indicator has already been Terminated, you can safely ignore errors
						if (State >= State.Terminated)
							return;
						
						Log("FibRetracementTargets", NinjaTrader.Cbi.LogLevel.Warning);
						
						Print(Time[0] + " " + e.ToString());
					}
					
					
					
					//Predictive Bear Fibonocci Retracement
					if(UsePredictiveRetracements && swingHighBarsAgo > predictiveLowBarsAgo && (latestSwingHigh - predictiveSwingLow >= MinSwingLength) && latestSwingHighBar != previousSwingHighBar && latestPredictiveSwingLowBar != previousPredictiveSwingLowBar)
					{
						if(RequireSwingTrend)
						{
							if(predictiveSwingLow > latestSwingLow)
								return;
						}
						bearFibLevel1 = ((High[swingHighBarsAgo] - Low[predictiveLowBarsAgo]) *LowFibPercent/100) + Low[predictiveLowBarsAgo];
						bearFibLevel2 = ((High[swingHighBarsAgo] - Low[predictiveLowBarsAgo]) *HighFibPercent/100) + Low[predictiveLowBarsAgo];
						
						if(predictiveBearRectangle != null)
						{
							RemoveDrawObject(predictiveBearRectangleTag);
        					predictiveBearRectangle = null;
						}
						predictiveBearRectangle = Draw.Rectangle(this, predictiveBearRectangleTag, true, predictiveLowBarsAgo, bearFibLevel1, -1*FibTargetWidth, bearFibLevel2, Brushes.OrangeRed, Brushes.Crimson, 0);
											
						previousPredictiveSwingLowBar = latestPredictiveSwingLowBar;
						
						//Set Bear properties for public use
						bearHasRetrace[0] = true;
						bearRetraceStartBarsAgo[0] = predictiveLowBarsAgo;
						bearRetraceFibLevel1[0] = bearFibLevel1;
						bearRetraceFibLevel2[0] = bearFibLevel2;
						
						
						bearFibDetected = true;
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
						
						if(predictiveBearRectangle != null)
						{
							RemoveDrawObject(predictiveBearRectangleTag);
        					predictiveBearRectangle = null;
						}
						Draw.Rectangle(this, CurrentBar + "-FibBox", true, swingLowBarsAgo, bearFibLevel1, -1*FibTargetWidth, bearFibLevel2, Brushes.Crimson, Brushes.Crimson, 0);
											
						previousSwingHighBar = latestSwingHighBar;
						
						//Set Bear properties for public use
						bearHasRetrace[0] = true;
						bearRetraceStartBarsAgo[0] = swingLowBarsAgo;
						bearRetraceFibLevel1[0] = bearFibLevel1;
						bearRetraceFibLevel2[0] = bearFibLevel2;
						
						
						bearFibDetected = true;
						
					}
					//Predictive Bull Fibonacci Retracement
					if (UsePredictiveRetracements && swingLowBarsAgo > predictiveHighBarsAgo && (predictiveSwingHigh - latestSwingLow >= MinSwingLength) && latestSwingLowBar != previousSwingLowBar && latestPredictiveSwingHighBar != previousPredictiveSwingHighBar)
					{
						if(RequireSwingTrend)
						{
							if(predictiveSwingHigh < latestSwingHigh)
								return;
						}
						
						
						bullFibLevel1 = High[predictiveHighBarsAgo] - ((High[predictiveHighBarsAgo] - Low[swingLowBarsAgo]) *LowFibPercent/100);
						bullFibLevel2 = High[predictiveHighBarsAgo] - ((High[predictiveHighBarsAgo] - Low[swingLowBarsAgo]) *HighFibPercent/100);
						
						if(predictiveBullRectangle != null)
						{
							RemoveDrawObject(predictiveBullRectangleTag);
        					predictiveBullRectangle = null;
						}
						
						predictiveBullRectangle = Draw.Rectangle(this, predictiveBullRectangleTag, true, predictiveHighBarsAgo, bullFibLevel1, -1*FibTargetWidth, bullFibLevel2, Brushes.LightGreen, Brushes.LimeGreen, 0);	
						
						previousPredictiveSwingHighBar = latestPredictiveSwingHighBar;
						
						//Set properties for public use
						bullHasRetrace[0] = true;
						bullRetraceStartBarsAgo[0] = predictiveHighBarsAgo;
						bullRetraceFibLevel1[0] = bullFibLevel1;
						bullRetraceFibLevel2[0] = bullFibLevel2;
						
						
						bullFibDetected = true;
						
					}
					//Bull Fibonocci Retracement
					if (swingLowBarsAgo > swingHighBarsAgo && (latestSwingHigh - latestSwingLow >= MinSwingLength) && latestSwingLowBar != previousSwingLowBar)
					{
						if(RequireSwingTrend)
						{
							if(latestSwingHigh < trendSwingHigh)
								return;
						}
						
						bullFibLevel1 = High[swingHighBarsAgo] - ((High[swingHighBarsAgo] - Low[swingLowBarsAgo]) *LowFibPercent/100);
						bullFibLevel2 = High[swingHighBarsAgo] - ((High[swingHighBarsAgo] - Low[swingLowBarsAgo]) *HighFibPercent/100);
						
						if(predictiveBullRectangle != null)
						{
							RemoveDrawObject(predictiveBullRectangleTag);
        					predictiveBullRectangle = null;
						}
						
						Draw.Rectangle(this, CurrentBar + "-FibBox", true, swingHighBarsAgo, bullFibLevel1, -1*FibTargetWidth, bullFibLevel2, Brushes.LimeGreen, Brushes.LimeGreen, 0);	
						
						previousSwingLowBar = latestSwingLowBar;
						
						//Set properties for public use
						bullHasRetrace[0] = true;
						bullRetraceStartBarsAgo[0] = swingHighBarsAgo;
						bullRetraceFibLevel1[0] = bullFibLevel1;
						bullRetraceFibLevel2[0] = bullFibLevel2;
					
						
						bullFibDetected = true;
						
					}  
						
				}
				//If you would like to see the structure of the DataSeries for your strategy development uncomment this.
//				Print(string.Format(
//				    "Time: {0}, bearHasRetrace: {1}, bearRetraceStartBarsAgo: {2}, bearRetraceFibLevel1: {3}, bearRetraceFibLevel2: {4}, bullHasRetrace: {5}, bullRetraceStartBarsAgo: {6}, bullRetraceFibLevel1: {7}, bullRetraceFibLevel2: {8}",
//				    Time[0],
//				    BearHasRetrace[0], 
//				    BearRetraceStartBarsAgo[0], 
//				    BearRetraceFibLevel1[0], 
//				    BearRetraceFibLevel2[0], 
//				    BullHasRetrace[0], 
//				    BullRetraceStartBarsAgo[0], 
//				    BullRetraceFibLevel1[0], 
//				    BullRetraceFibLevel2[0]
//				));
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
		[Display(Name="PredictiveSwingStrength", Order=2, GroupName="Parameters")]
		public int PredictiveSwingStrength
		{ get; set; }
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="MinSwingLength", Order=3, GroupName="Parameters")]
		public int MinSwingLength
		{ get; set; }
		[NinjaScriptProperty]
		[Range(1, double.MaxValue)]
		[Display(Name="LowFibPercent", Order=4, GroupName="Parameters")]
		public double LowFibPercent
		{ get; set; }
		[NinjaScriptProperty]
		[Range(1, double.MaxValue)]
		[Display(Name="HighFibPercent", Order=5, GroupName="Parameters")]
		public double HighFibPercent
		{ get; set; }
		[NinjaScriptProperty]
		[Display(Name="RequireSwingTrend", Order=6, GroupName="Parameters")]
		public bool RequireSwingTrend
		{ get; set; }
		[NinjaScriptProperty]
		[Display(Name="UsePredictiveRetracements", Order=7, GroupName="Parameters")]
		public bool UsePredictiveRetracements
		{ get; set; }
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="FibTargetWidth", Order=8, GroupName="Parameters")]
		public int FibTargetWidth
		{ get; set; }
		
		
		[Browsable(false)]
		[XmlIgnore()]
		public Series<bool> BearHasRetrace
		{
			get
			{
				Update();
				return bearHasRetrace;
			}
		}
		[Browsable(false)]
		[XmlIgnore()]
		public Series<int> BearRetraceStartBarsAgo
		{
			get
			{
				Update();
				return bearRetraceStartBarsAgo;
			}
		}
		[Browsable(false)]
		[XmlIgnore()]
		public Series<double> BearRetraceFibLevel1
		{
			get
			{
				Update();
				return bearRetraceFibLevel1;
			}
		}
		[Browsable(false)]
		[XmlIgnore()]
		public Series<double> BearRetraceFibLevel2
		{
			get
			{
				Update();
				return bearRetraceFibLevel2;
			}
		}
		
		[Browsable(false)]
		[XmlIgnore()]
		public Series<bool> BullHasRetrace
		{
			get
			{
				Update();
				return bullHasRetrace;
			}
		}
		[Browsable(false)]
		[XmlIgnore()]
		public Series<int> BullRetraceStartBarsAgo
		{
			get
			{
				Update();
				return bullRetraceStartBarsAgo;
			}
		}
		[Browsable(false)]
		[XmlIgnore()]
		public Series<double> BullRetraceFibLevel1
		{
			get
			{
				Update();
				return bullRetraceFibLevel1;
			}
		}
		[Browsable(false)]
		[XmlIgnore()]
		public Series<double> BullRetraceFibLevel2
		{
			get
			{
				Update();
				return bullRetraceFibLevel2;
			}
		}
		
		
		#endregion
	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private FibRetracementTargets[] cacheFibRetracementTargets;
		public FibRetracementTargets FibRetracementTargets(int swingStrength, int predictiveSwingStrength, int minSwingLength, double lowFibPercent, double highFibPercent, bool requireSwingTrend, bool usePredictiveRetracements, int fibTargetWidth)
		{
			return FibRetracementTargets(Input, swingStrength, predictiveSwingStrength, minSwingLength, lowFibPercent, highFibPercent, requireSwingTrend, usePredictiveRetracements, fibTargetWidth);
		}

		public FibRetracementTargets FibRetracementTargets(ISeries<double> input, int swingStrength, int predictiveSwingStrength, int minSwingLength, double lowFibPercent, double highFibPercent, bool requireSwingTrend, bool usePredictiveRetracements, int fibTargetWidth)
		{
			if (cacheFibRetracementTargets != null)
				for (int idx = 0; idx < cacheFibRetracementTargets.Length; idx++)
					if (cacheFibRetracementTargets[idx] != null && cacheFibRetracementTargets[idx].SwingStrength == swingStrength && cacheFibRetracementTargets[idx].PredictiveSwingStrength == predictiveSwingStrength && cacheFibRetracementTargets[idx].MinSwingLength == minSwingLength && cacheFibRetracementTargets[idx].LowFibPercent == lowFibPercent && cacheFibRetracementTargets[idx].HighFibPercent == highFibPercent && cacheFibRetracementTargets[idx].RequireSwingTrend == requireSwingTrend && cacheFibRetracementTargets[idx].UsePredictiveRetracements == usePredictiveRetracements && cacheFibRetracementTargets[idx].FibTargetWidth == fibTargetWidth && cacheFibRetracementTargets[idx].EqualsInput(input))
						return cacheFibRetracementTargets[idx];
			return CacheIndicator<FibRetracementTargets>(new FibRetracementTargets(){ SwingStrength = swingStrength, PredictiveSwingStrength = predictiveSwingStrength, MinSwingLength = minSwingLength, LowFibPercent = lowFibPercent, HighFibPercent = highFibPercent, RequireSwingTrend = requireSwingTrend, UsePredictiveRetracements = usePredictiveRetracements, FibTargetWidth = fibTargetWidth }, input, ref cacheFibRetracementTargets);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.FibRetracementTargets FibRetracementTargets(int swingStrength, int predictiveSwingStrength, int minSwingLength, double lowFibPercent, double highFibPercent, bool requireSwingTrend, bool usePredictiveRetracements, int fibTargetWidth)
		{
			return indicator.FibRetracementTargets(Input, swingStrength, predictiveSwingStrength, minSwingLength, lowFibPercent, highFibPercent, requireSwingTrend, usePredictiveRetracements, fibTargetWidth);
		}

		public Indicators.FibRetracementTargets FibRetracementTargets(ISeries<double> input , int swingStrength, int predictiveSwingStrength, int minSwingLength, double lowFibPercent, double highFibPercent, bool requireSwingTrend, bool usePredictiveRetracements, int fibTargetWidth)
		{
			return indicator.FibRetracementTargets(input, swingStrength, predictiveSwingStrength, minSwingLength, lowFibPercent, highFibPercent, requireSwingTrend, usePredictiveRetracements, fibTargetWidth);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.FibRetracementTargets FibRetracementTargets(int swingStrength, int predictiveSwingStrength, int minSwingLength, double lowFibPercent, double highFibPercent, bool requireSwingTrend, bool usePredictiveRetracements, int fibTargetWidth)
		{
			return indicator.FibRetracementTargets(Input, swingStrength, predictiveSwingStrength, minSwingLength, lowFibPercent, highFibPercent, requireSwingTrend, usePredictiveRetracements, fibTargetWidth);
		}

		public Indicators.FibRetracementTargets FibRetracementTargets(ISeries<double> input , int swingStrength, int predictiveSwingStrength, int minSwingLength, double lowFibPercent, double highFibPercent, bool requireSwingTrend, bool usePredictiveRetracements, int fibTargetWidth)
		{
			return indicator.FibRetracementTargets(input, swingStrength, predictiveSwingStrength, minSwingLength, lowFibPercent, highFibPercent, requireSwingTrend, usePredictiveRetracements, fibTargetWidth);
		}
	}
}

#endregion
