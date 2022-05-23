using System;
using System.Collections.Generic;
using System.Linq;
using MyPlotHelper;

namespace Arithmatics;

public class FramebasedSlotAloha
{
    public static void PlotSlotCount()
    {
        const int emulateCount = 5;
        const int min = 50;
        const int max = 2000;
        var X = new double[max-min];
        var Y = new double[max-min];
        for(var i = min; i < max; i++)
        {
            var occupyRatio = new float[emulateCount];
            for (int j = 0; j < emulateCount; j++)
            {
                // Get The Average
                occupyRatio[j] = Do(500,1,i);
            }
            X[i - min] = i;
            Y[i - min] = occupyRatio.Average();
        }
        
        var plot = new MatlabPlot();
			
        plot.Plot2(X, Y)
            .AddLabel(MatlabPlot.ReadyPlot.LabelLocation.X, "Slot Count")
            .AddLabel(MatlabPlot.ReadyPlot.LabelLocation.Y, "Channel Occupy Ratio")
            .AddLegend("Emulate The Relation Of Slot Count And The Channel Occupy Ratio")
            .Execute();
    }
    public static void PlotTagCount()
    {
        const int emulateCount = 50;
        var X = new double[250];
        var Y = new double[250];
        for(var i = 1; i <= 250; i++)
        {
            var occupyRatio = new float[emulateCount];
            for (var j = 0; j < emulateCount; j++)
            {
                // Get The Average
                occupyRatio[j] = Do(i,1,40);
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
    public static float Do(int tagCount, float slotSize = 1, int frameSize = 10)
    {
        const int messageLength = 1;
        // Input Properties
        var Rand = () => new Random(Guid.NewGuid().GetHashCode()).Next(frameSize);

        // Init Attrs
        // var totalTime = 0;
        var totalFrame = 0;
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
            // Console.WriteLine($"////////////////////////////////// frame = {totalFrame}");
            var isRestTags = false;
            // active all tags
            foreach (var tag in tags)
            {
                tag.IsActive = true;
            }
            /// for every slot in frame
            for (var slot = 0; slot < frameSize; slot++)
            {
                // Console.WriteLine($"/////////////////////////// slot = {slot}");
                isRestTags = false;
                // for every tag
                for (var i = 0; i < tags.Count; i++)
                {
                    // Thread.Sleep(1);
                    var tag = tags[i];
                    // check tag is active
                    if (!tag.IsActive) continue;
                    // check tag is ready to send
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
                    
                    // if encounter conflict
                    if (isChannelOccupied > 0 && occupyTagIndex != i)
                    {
                        tag.RestWaitTime = Rand();
                        tag.MessageLength = messageLength;
                        tag.IsActive = false;
                        // Console.WriteLine($"Tag {i} RestTime Reset = {tag.RestWaitTime}");
                        if (tags[occupyTagIndex].RestWaitTime <= 0)
                        {
                            tags[occupyTagIndex].RestWaitTime = Rand();
                            tags[occupyTagIndex].MessageLength = messageLength;
                            tags[occupyTagIndex].IsActive = false;
                            // Console.WriteLine($"Tag {occupyTagIndex} RestTime Reset = {tags[occupyTagIndex].RestWaitTime}");
                        }
                        
                        continue;
                    }

                    // occupy the channel
                    // Console.WriteLine($"tag {i} MessageLength = {tag.MessageLength}");
                    occupyTagIndex = i;

                    isChannelOccupied = tag.MessageLength;
                    tag.MessageLength--;
                } // for every tag
                

                isChannelOccupied = 0;
            }// for every slot
            
            if (!isRestTags)
            {
                totalFrame++;
                // Console.WriteLine($"Is Rest Tags ? {isRestTags}");
                break;
            }

            totalFrame++;
            /// 
        } // while true

        var occupyRatio = (tagCount * ((float)messageLength / slotSize)) / (totalFrame * frameSize);

        Console.WriteLine("The Consequence: ");
        Console.WriteLine($"Total Frame = {totalFrame}");
        Console.WriteLine($"Tag Count = {tagCount}");
        Console.WriteLine($"Message Length = {messageLength}");
        Console.WriteLine($"Frame Size = {frameSize}");
        Console.WriteLine($"Occupy Ratio = {occupyRatio}");

        return occupyRatio;
    }
}