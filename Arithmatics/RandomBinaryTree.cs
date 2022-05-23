using MyPlotHelper;

namespace Arithmatics;

public class RandomBinaryTree
{
    
    public static void PlotTagCount()
    {
        const int emulateCount = 10;
        var X = new double[250];
        var Y = new double[250];
        for(var i = 1; i <= 250; i++)
        {
            var totalTime = new float[emulateCount];
            for (var j = 0; j < emulateCount; j++)
            {
                // Get The Average
                totalTime[j] = Do(i,1).TotalUnitTime;
            }
            X[i - 1] = i;
            Y[i - 1] = totalTime.Average();
        }
        
        var plot = new MatlabPlot();
			
        plot.Plot2(X, Y)
            .AddLabel(MatlabPlot.ReadyPlot.LabelLocation.X, "Tag Count")
            .AddLabel(MatlabPlot.ReadyPlot.LabelLocation.Y, "Unit Time")
            .AddLegend("Emulate The Relation Of Tag Count And The Unit Time")
            .Execute();
    }
    public static void PlotTagIDLength()
    {
        const int emulateCount = 10;
        var X = new double[250];
        var Y = new double[250];
        for(var i = 1; i <= 250; i++)
        {
            var occupyRatio = new float[emulateCount];
            for (var j = 0; j < emulateCount; j++)
            {
                // Get The Average
                occupyRatio[j] = Do(50,i).OccupyRatio;
            }
            X[i - 1] = i;
            Y[i - 1] = occupyRatio.Average();
        }
        
        var plot = new MatlabPlot();
			
        plot.Plot2(X, Y)
            .AddLabel(MatlabPlot.ReadyPlot.LabelLocation.X, "Tag ID Length")
            .AddLabel(MatlabPlot.ReadyPlot.LabelLocation.Y, "Occupy Ratio")
            .AddLegend("Emulate The Relation Of Tag ID Length And The Occupy Ratio")
            .Execute();
    }

    public struct Consequence
    {
        public int TotalUnitTime { get; set; }
        public float OccupyRatio { get; set; }
    }
    public static Consequence Do(int tagCount, int tagIDLength = 1)
    {
        int messageLength = tagIDLength;
        // Input Properties
        var Rand = () => new Random(Guid.NewGuid().GetHashCode()).Next(2);
        
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
                RestWaitTime = 0
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
                    tag.RestWaitTime = isChannelOccupied switch
                    {
                        0 => tag.RestWaitTime - 1,
                        _ => tag.RestWaitTime + 1
                    };
                    // Console.WriteLine($"Tag {i} RestTime = {tag.RestWaitTime}");
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
                // Console.WriteLine($"Is Rest Tags ? {isRestTags}");
                break;
            }

            isChannelOccupied = 0;
            
            totalTime++;
        } // while true

        var occupyRatio = (tagCount * ((float)messageLength)) / totalTime;

        Console.WriteLine("The Consequence: ");
        Console.WriteLine($"Total Time = {totalTime}");
        Console.WriteLine($"Tag Count = {tagCount}");
        Console.WriteLine($"Message Length = {messageLength}");
        Console.WriteLine($"Occupy Ratio = {occupyRatio}");

        return new Consequence()
        {
            TotalUnitTime = totalTime,
            OccupyRatio = occupyRatio
        };
    }
}