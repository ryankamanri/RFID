using System;
using System.Collections.Generic;
using System.Linq;
using MyPlotHelper;

namespace Arithmatics;

public class SlotAloha
{
    
    public static void PlotTagCount()
    {
        const int emulateCount = 5;
        var X = new double[250];
        var Y = new double[250];
        for(var i = 1; i <= 200; i++)
        {
            var occupyRatio = new float[emulateCount];
            for (int j = 0; j < emulateCount; j++)
            {
                // Get The Average
                occupyRatio[j] = Do(i,1,1,50);
            }
            X[i - 1] = i;
            Y[i - 1] = occupyRatio.Average();
        }
        
        var plot = new MatlabPlot();
			
        plot.Plot2(X, Y)
            .AddLabel(MatlabPlot.ReadyPlot.LabelLocation.X, "Tag Count")
            .AddLabel(MatlabPlot.ReadyPlot.LabelLocation.Y, "Channel Occupy Ratio")
            .AddLegend("Emulate The Relation Of Tag Count And The Channel Occupy Ratio")
            .Execute();
    }
    
    public static void PlotSlotSize()
    {
        const int emulateCount = 5;
        var X = new double[50];
        var Y = new double[50];
        for(var i = 1; i <= 50; i++)
        {
            var occupyRatio = new float[emulateCount];
            for (int j = 0; j < emulateCount; j++)
            {
                // Get The Average
                occupyRatio[j] = Do(50,(float)(0.9 + 0.1 * i),1,50);
            }
            X[i - 1] = i;
            Y[i - 1] = occupyRatio.Average();
        }
        
        var plot = new MatlabPlot();
			
        plot.Plot2(X, Y)
            .AddLabel(MatlabPlot.ReadyPlot.LabelLocation.X, "Slot Size")
            .AddLabel(MatlabPlot.ReadyPlot.LabelLocation.Y, "Channel Occupy Ratio")
            .AddLegend("Emulate The Relation Of Slot Size And The Channel Occupy Ratio")
            .Execute();
    }
    
    public static float Do(int tagCount, float slotSize = 1, int minWaitTime = 1, int maxWaitTime = 10)
    {
        const int messageLength = 1;
        // Input Properties
        var Rand = () => new Random(Guid.NewGuid().GetHashCode()).Next(minWaitTime, maxWaitTime);
        
        // Init Attrs
        var totalTime = 0;
        var occupyTagIndex = 0;
        var isChannelOccupied = 0;
        // Init Tags
        var tags = new List<Tag>();
        for (var i = 0; i < tagCount; i++)
        {
            tags.Add(new Tag()
            {
                MessageLength = messageLength,
                RestWaitTime = Rand()
            });
        }

        while (true)
        {
            // Console.WriteLine("///////////////////");
            var isRestTags = false;
            for (var i = 0; i < tags.Count; i++)
            {
                // Thread.Sleep(1);
                var tag = tags[i];
                if (tag.RestWaitTime > 0)
                {
                    isRestTags = true;
                    // Console.WriteLine($"Tag {i} RestTime = {tag.RestWaitTime}");
                    tag.RestWaitTime--;
                    continue;
                }
                
                // check is it rest tags
                if (tag.MessageLength > 0)
                {
                    isRestTags = true;
                }
                else
                {
                    // done send
                    continue;
                }

                if (isChannelOccupied > 0 && occupyTagIndex != i)
                {
                    tag.RestWaitTime = Rand();
                    tag.MessageLength = messageLength;
                    // Console.WriteLine($"Tag {i} RestTime Reset = {tag.RestWaitTime}");
                    if (tags[occupyTagIndex].RestWaitTime <= 0)
                    {
                        tags[occupyTagIndex].RestWaitTime = Rand();
                        tags[occupyTagIndex].MessageLength = messageLength;
                        // Console.WriteLine($"Tag {occupyTagIndex} RestTime Reset = {tags[occupyTagIndex].RestWaitTime}");
                    }
                    // isChannelOccupied = 0;
                    continue;
                        
                }

                // occupy the channel
                // Console.WriteLine($"tag {i} MessageLength = {tag.MessageLength}");
                occupyTagIndex = i;
                
                isChannelOccupied = tag.MessageLength;
                tag.MessageLength--;
                
                
            } // for tags

            if (!isRestTags)
            {
                Console.WriteLine($"Is Rest Tags ? {isRestTags}");
                break;
            }

            isChannelOccupied = 0;
            
            totalTime++;
        } // while true

        var occupyRatio = (tagCount * ((float)messageLength / slotSize)) / totalTime;

        Console.WriteLine("The Consequence: ");
        Console.WriteLine($"Total Time = {totalTime}");
        Console.WriteLine($"Tag Count = {tagCount}");
        Console.WriteLine($"Message Length = {messageLength}");
        Console.WriteLine($"Occupy Ratio = {occupyRatio}");

        return occupyRatio;
    }
}