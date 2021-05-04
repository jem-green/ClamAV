using System;
using System.Collections.Generic;
using System.Text;

namespace UpdateClam
{
    public class Progress
    {
        private double start = 0;
        private double finish = 100;
        private double current = 0;
        private int width = 50;
        private ChangeType changes = ChangeType.any;
        private ProgressType progress = ProgressType.text;
        private bool showPercentage = true;
        private bool visible = true;
        private int position = -1;
        private int percentage = -1;
        private int lastPosition = -1;
        private int lastPercentage = -1;
        private bool positionChanged = false;
        private bool percentageChanged = false;
        private char startCharacter = '[';
        private char endCharacter = ']';
        private char fillCharacter = '=';
        private char emptyCharacter = ' ';

        public enum ChangeType
        {
            none = 0,
            percentage = 1,
            position = 2,
            any = 3,
        }

        public enum ProgressType
        {
            text = 0,
            solid = 1,  // uses character 
        }

        public Progress()
        {
        }
        public Progress(double start, double finish)
        {
            this.start = start;
            this.finish = finish;
        }

        public ChangeType Change
        {
            set
            {
                changes = value;
            }
        }
        public ProgressType Type
        {
            set
            {
                progress = value;
            }
        }

        public double Start
        {
            set
            {
                start = value;
            }
            get
            {
                return (start);
            }
        }

        public double Finish
        {
            set
            {
                finish = value;
            }
            get
            {
                return (finish);
            }
        }

        public double Current
        {
            set
            {
                current = value;
            }
            get
            {
                return (current);
            }
        }

        public int Width
        {
            set
            {
                width = value;
            }
            get
            {
                return (width);
            }
        }
        public bool Visible
        {
            set
            {
                visible = value;
            }
            get
            {
                return (visible);
            }
        }
        public bool hasChanged
        {
            get
            {
                if (changes == ChangeType.any)
                {
                    return (percentageChanged || positionChanged == true);
                }
                else if (changes == ChangeType.percentage)
                {
                    return (percentageChanged);
                }
                else if (changes == ChangeType.position)
                {
                    return (positionChanged);
                }
                else
                {
                    return (false);
                }
            }
        }

        public void Update(double value)
        {
            this.current = value;
            Update();
        }

        public void Update()
        {
            try
            {
                double r = 0;
                try
                {
                    r = (current - start) / (finish - start);
                    if (r < 0)
                    {
                        r = 0;
                    }
                    if (r > 1)
                    {
                        r = 1;
                    }
                }
                catch
                {
                    r = 0;
                }

                try
                {
                    if (progress == ProgressType.text)
                    {
                        position = Convert.ToInt32(Math.Floor(width * r));
                    }
                    else
                    {
                        position = Convert.ToInt32(Math.Floor(2 * width * r));
                    }
                }
                catch
                {
                    position = lastPosition;
                }
              
                if (position == lastPosition)
                {
                    positionChanged = false;
                }
                else
                {
                    lastPosition = position;
                    positionChanged = true;
                }

                try
                {
                    percentage = Convert.ToInt32(Math.Floor(r * 100));
                    if (percentage == lastPercentage)
                    {
                        percentageChanged = false;
                    }
                    else
                    {
                        lastPercentage = percentage;
                        percentageChanged = true;
                    }
                }
                catch
                {
                    percentageChanged = false;
                }

            }
            catch
            {
                percentageChanged = false;
            }
        }

        public string Show(double current)
        {
            Update(current);
            return (Show());
        }

        public string Show()
        {
            string bar = "";
            if (visible == true)
            {
                StringBuilder stringBuilder = new StringBuilder();
                if (progress == ProgressType.text)
                {
                    stringBuilder.Append(startCharacter);
                    stringBuilder.Append(fillCharacter, position);
                    stringBuilder.Append(emptyCharacter, width - position);
                    stringBuilder.Append(endCharacter);
                    if (showPercentage == true)
                    {
                        stringBuilder.Append(" " + percentage.ToString() + "%");
                    }
                }
                else
                {
                    stringBuilder.Append('[');
                    if ((position % 2) == 0)
                    {
                        stringBuilder.Append('█', position / 2);
                    }
                    else
                    {
                        if (position >= 2)
                        {
                            stringBuilder.Append('█', position / 2 - 1);
                        }
                        stringBuilder.Append('▌');
                    }
                    stringBuilder.Append(' ', width - position / 2);

                    stringBuilder.Append(']');
                    if (showPercentage == true)
                    {
                        stringBuilder.Append(" " + percentage.ToString() + "%");
                    }
                }
                bar = stringBuilder.ToString();
            }
            return (bar);
        }
    }
}
        