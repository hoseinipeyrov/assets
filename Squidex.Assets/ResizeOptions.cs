// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System.Globalization;
using System.Text;

namespace Squidex.Assets
{
    public sealed class ResizeOptions
    {
        public ImageFormat? Format { get; set; }

        public ResizeMode Mode { get; set; }

        public int? TargetWidth { get; set; }

        public int? TargetHeight { get; set; }

        public int? Quality { get; set; }

        public float? FocusX { get; set; }

        public float? FocusY { get; set; }

        public string? Background { get; set; }

        public IEnumerable<KeyValuePair<string, string>>? ExtraParameters { get; set; }

        public bool IsValid
        {
            get { return TargetWidth > 0 || TargetHeight > 0 || Quality > 0 || Format != null; }
        }

        public IEnumerable<KeyValuePair<string, string>> ToParameters()
        {
            if (Mode != default)
            {
                yield return new KeyValuePair<string, string>("mode", Mode.ToString());
            }

            if (Format != null)
            {
                yield return new KeyValuePair<string, string>("format", Format.ToString());
            }

            if (TargetWidth != null)
            {
                yield return new KeyValuePair<string, string>("targetWidth", TargetWidth.ToString());
            }

            if (TargetHeight != null)
            {
                yield return new KeyValuePair<string, string>("targetHeight", TargetHeight.ToString());
            }

            if (Quality != null)
            {
                yield return new KeyValuePair<string, string>("quality", Quality.ToString());
            }

            if (FocusX != null)
            {
                yield return new KeyValuePair<string, string>("focusX", FocusX.ToString());
            }

            if (FocusY != null)
            {
                yield return new KeyValuePair<string, string>("focusY", FocusY.ToString());
            }

            if (Background != null)
            {
                yield return new KeyValuePair<string, string>("background", Background);
            }

            if (ExtraParameters != null)
            {
                foreach (var kvp in ExtraParameters)
                {
                    yield return kvp;
                }
            }
        }

        public static ResizeOptions Parse(Dictionary<string, string> parameters)
        {
            var result = new ResizeOptions();

            if (parameters.TryGetValue("mode", out var temp) && Enum.TryParse<ResizeMode>(temp, out var mode))
            {
                result.Mode = mode;
            }

            if (parameters.TryGetValue("format", out temp) && Enum.TryParse<ImageFormat>(temp, out var format))
            {
                result.Format = format;
            }

            if (parameters.TryGetValue("targetWidth", out temp) && int.TryParse(temp, NumberStyles.Integer, CultureInfo.InvariantCulture, out var targetWidth))
            {
                result.TargetWidth = targetWidth;
            }

            if (parameters.TryGetValue("targetHeight", out temp) && int.TryParse(temp, NumberStyles.Integer, CultureInfo.InvariantCulture, out var targetHeight))
            {
                result.TargetHeight = targetHeight;
            }

            if (parameters.TryGetValue("quality", out temp) && int.TryParse(temp, NumberStyles.Integer, CultureInfo.InvariantCulture, out var quality))
            {
                result.Quality = quality;
            }

            if (parameters.TryGetValue("focusX", out temp) && float.TryParse(temp, NumberStyles.Integer, CultureInfo.InvariantCulture, out var focusX))
            {
                result.FocusX = focusX;
            }

            if (parameters.TryGetValue("focusY", out temp) && float.TryParse(temp, NumberStyles.Integer, CultureInfo.InvariantCulture, out var focusY))
            {
                result.FocusY = focusY;
            }

            if (parameters.TryGetValue("background", out temp))
            {
                result.Background = temp;
            }

            return result;
        }

        public override string ToString()
        {
            var sb = new StringBuilder();

            sb.Append(TargetWidth);
            sb.Append("_");
            sb.Append(TargetHeight);
            sb.Append("_");
            sb.Append(Mode);

            if (Quality.HasValue)
            {
                sb.Append("_");
                sb.Append(Quality);
            }

            if (FocusX.HasValue)
            {
                sb.Append("_focusX_");
                sb.Append(FocusX);
            }

            if (FocusY.HasValue)
            {
                sb.Append("_focusY_");
                sb.Append(FocusY);
            }

            if (Format != null)
            {
                sb.Append("_format_");
                sb.Append(Format.ToString());
            }

            if (!string.IsNullOrWhiteSpace(Background))
            {
                sb.Append("_background_");
                sb.Append(Background);
            }

            return sb.ToString();
        }
    }
}
