﻿using System;
using System.Collections.Generic;

namespace TVTComment.Model.ChatService
{
    public interface IChatService : IDisposable
    {
        string Name { get; }
        IReadOnlyList<ChatCollectServiceEntry.IChatCollectServiceEntry> ChatCollectServiceEntries { get; }
        IReadOnlyList<IChatTrendServiceEntry> ChatTrendServiceEntries { get; }
    }
}
