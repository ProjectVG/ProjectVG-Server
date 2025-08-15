namespace ProjectVG.Common.Constants
{
    public static class LLMModelInfo
    {
        public static class GPT5
        {
            public const string Name = "gpt-5";
            public static class Price
            {
                public const double Input = 1.25;
                public const double CachedInput = 0.125;
                public const double Output = 10.00;
            }
        }

        public static class GPT5Mini
        {
            public const string Name = "gpt-5-mini";
            public static class Price
            {
                public const double Input = 0.25;
                public const double CachedInput = 0.025;
                public const double Output = 2.00;
            }
        }

        public static class GPT5Nano
        {
            public const string Name = "gpt-5-nano";
            public static class Price
            {
                public const double Input = 0.05;
                public const double CachedInput = 0.005;
                public const double Output = 0.40;
            }
        }

        public static class GPT5ChatLatest
        {
            public const string Name = "gpt-5-chat-latest";
            public static class Price
            {
                public const double Input = 1.25;
                public const double CachedInput = 0.125;
                public const double Output = 10.00;
            }
        }

        public static class GPT41
        {
            public const string Name = "gpt-4.1";
            public static class Price
            {
                public const double Input = 2.00;
                public const double CachedInput = 0.50;
                public const double Output = 8.00;
            }
        }

        public static class GPT41Mini
        {
            public const string Name = "gpt-4.1-mini";
            public static class Price
            {
                public const double Input = 0.40;
                public const double CachedInput = 0.10;
                public const double Output = 1.60;
            }
        }

        public static class GPT41Nano
        {
            public const string Name = "gpt-4.1-nano";
            public static class Price
            {
                public const double Input = 0.10;
                public const double CachedInput = 0.025;
                public const double Output = 0.40;
            }
        }

        public static class GPT4o
        {
            public const string Name = "gpt-4o";
            public static class Price
            {
                public const double Input = 2.50;
                public const double CachedInput = 1.25;
                public const double Output = 10.00;
            }
        }

        public static class GPT4o20240513
        {
            public const string Name = "gpt-4o-2024-05-13";
            public static class Price
            {
                public const double Input = 5.00;
                public const double Output = 15.00;
            }
        }

        public static class GPT4oAudioPreview
        {
            public const string Name = "gpt-4o-audio-preview";
            public static class Price
            {
                public const double Input = 2.50;
                public const double Output = 10.00;
            }
        }

        public static class GPT4oRealtimePreview
        {
            public const string Name = "gpt-4o-realtime-preview";
            public static class Price
            {
                public const double Input = 5.00;
                public const double CachedInput = 2.50;
                public const double Output = 20.00;
            }
        }

        public static class GPT4oMini
        {
            public const string Name = "gpt-4o-mini";
            public static class Price
            {
                public const double Input = 0.15;
                public const double CachedInput = 0.075;
                public const double Output = 0.60;
            }
        }

        public static class GPT4oMiniAudioPreview
        {
            public const string Name = "gpt-4o-mini-audio-preview";
            public static class Price
            {
                public const double Input = 0.15;
                public const double Output = 0.60;
            }
        }

        public static class GPT4oMiniRealtimePreview
        {
            public const string Name = "gpt-4o-mini-realtime-preview";
            public static class Price
            {
                public const double Input = 0.60;
                public const double CachedInput = 0.30;
                public const double Output = 2.40;
            }
        }

        public static class O1
        {
            public const string Name = "o1";
            public static class Price
            {
                public const double Input = 15.00;
                public const double CachedInput = 7.50;
                public const double Output = 60.00;
            }
        }

        public static class O1Pro
        {
            public const string Name = "o1-pro";
            public static class Price
            {
                public const double Input = 150.00;
                public const double Output = 600.00;
            }
        }

        public static class O3Pro
        {
            public const string Name = "o3-pro";
            public static class Price
            {
                public const double Input = 20.00;
                public const double Output = 80.00;
            }
        }

        public static class O3
        {
            public const string Name = "o3";
            public static class Price
            {
                public const double Input = 2.00;
                public const double CachedInput = 0.50;
                public const double Output = 8.00;
            }
        }

        public static class GPT35Turbo
        {
            public const string Name = "gpt-3.5-turbo";
            public static class Price
            {
                public const double Input = 0.50;
                public const double Output = 1.50;
            }
        }

        public static class GPT4
        {
            public const string Name = "gpt-4";
            public static class Price
            {
                public const double Input = 30.00;
                public const double Output = 60.00;
            }
        }

        public static class DefaultSettings
        {
            public const string DefaultModel = GPT4oMini.Name;
            public const int DefaultMaxTokens = 1000;
            public const float DefaultTemperature = 0.7f;
        }

        public static double GetInputCost(string model)
        {
            return model switch
            {
                GPT5.Name => GPT5.Price.Input,
                GPT5Mini.Name => GPT5Mini.Price.Input,
                GPT5Nano.Name => GPT5Nano.Price.Input,
                GPT5ChatLatest.Name => GPT5ChatLatest.Price.Input,
                GPT41.Name => GPT41.Price.Input,
                GPT41Mini.Name => GPT41Mini.Price.Input,
                GPT41Nano.Name => GPT41Nano.Price.Input,
                GPT4o.Name => GPT4o.Price.Input,
                GPT4o20240513.Name => GPT4o20240513.Price.Input,
                GPT4oAudioPreview.Name => GPT4oAudioPreview.Price.Input,
                GPT4oRealtimePreview.Name => GPT4oRealtimePreview.Price.Input,
                GPT4oMini.Name => GPT4oMini.Price.Input,
                GPT4oMiniAudioPreview.Name => GPT4oMiniAudioPreview.Price.Input,
                GPT4oMiniRealtimePreview.Name => GPT4oMiniRealtimePreview.Price.Input,
                O1.Name => O1.Price.Input,
                O1Pro.Name => O1Pro.Price.Input,
                O3Pro.Name => O3Pro.Price.Input,
                O3.Name => O3.Price.Input,
                GPT35Turbo.Name => GPT35Turbo.Price.Input,
                GPT4.Name => GPT4.Price.Input,
                _ => GPT4oMini.Price.Input // 기본값
            };
        }

        public static double GetOutputCost(string model)
        {
            return model switch
            {
                GPT5.Name => GPT5.Price.Output,
                GPT5Mini.Name => GPT5Mini.Price.Output,
                GPT5Nano.Name => GPT5Nano.Price.Output,
                GPT5ChatLatest.Name => GPT5ChatLatest.Price.Output,
                GPT41.Name => GPT41.Price.Output,
                GPT41Mini.Name => GPT41Mini.Price.Output,
                GPT41Nano.Name => GPT41Nano.Price.Output,
                GPT4o.Name => GPT4o.Price.Output,
                GPT4o20240513.Name => GPT4o20240513.Price.Output,
                GPT4oAudioPreview.Name => GPT4oAudioPreview.Price.Output,
                GPT4oRealtimePreview.Name => GPT4oRealtimePreview.Price.Output,
                GPT4oMini.Name => GPT4oMini.Price.Output,
                GPT4oMiniAudioPreview.Name => GPT4oMiniAudioPreview.Price.Output,
                GPT4oMiniRealtimePreview.Name => GPT4oMiniRealtimePreview.Price.Output,
                O1.Name => O1.Price.Output,
                O1Pro.Name => O1Pro.Price.Output,
                O3Pro.Name => O3Pro.Price.Output,
                O3.Name => O3.Price.Output,
                GPT35Turbo.Name => GPT35Turbo.Price.Output,
                GPT4.Name => GPT4.Price.Output,
                _ => GPT4oMini.Price.Output // 기본값
            };
        }

        public static double GetCachedInputCost(string model)
        {
            return model switch
            {
                GPT5.Name => GPT5.Price.CachedInput,
                GPT5Mini.Name => GPT5Mini.Price.CachedInput,
                GPT5Nano.Name => GPT5Nano.Price.CachedInput,
                GPT5ChatLatest.Name => GPT5ChatLatest.Price.CachedInput,
                GPT41.Name => GPT41.Price.CachedInput,
                GPT41Mini.Name => GPT41Mini.Price.CachedInput,
                GPT41Nano.Name => GPT41Nano.Price.CachedInput,
                GPT4o.Name => GPT4o.Price.CachedInput,
                GPT4oRealtimePreview.Name => GPT4oRealtimePreview.Price.CachedInput,
                GPT4oMini.Name => GPT4oMini.Price.CachedInput,
                GPT4oMiniRealtimePreview.Name => GPT4oMiniRealtimePreview.Price.CachedInput,
                O1.Name => O1.Price.CachedInput,
                O3.Name => O3.Price.CachedInput,
                _ => GetInputCost(model) * 0.1 // 기본적으로 입력 비용의 10%
            };
        }
    }
}
