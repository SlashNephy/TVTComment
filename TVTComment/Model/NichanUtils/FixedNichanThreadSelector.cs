﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TVTComment.Model.NichanUtils
{
    class FixedNichanThreadSelector : INichanThreadSelector
    {
        public IEnumerable<string> Uris { get; }

        public FixedNichanThreadSelector(IEnumerable<string> uris)
        {
            this.Uris = uris;
        }

        public async Task<IEnumerable<string>> Get(ChannelInfo channel, DateTime time)
        {
            return this.Uris;
        }
    }
}
