using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace CasCap.Models
{
	public class Event
	{
		public Event(EventType type, Result[] results, string message)
		{
			this.Results = results;
			this.Message = message ?? throw new ArgumentNullException(nameof(message));
			this.Type = type; 
		}

		public string ToJson() => JsonConvert.SerializeObject(this);

		public Result[] Results { get; set; }
		//public string[] Paths { get; set; }
		//public int Progress { get; set; }
		public string Message { get; set; }
		public EventType Type { get; set; }
		public override bool Equals(object? obj)
		{
			return obj is Event @event &&
				   this.Results.SequenceEqual(@event.Results) &&
				   Message == @event.Message &&
				   Type == @event.Type;
		}
	}

	public class Result
	{
		public Result(string filePath, int progress)
		{
			this.filePath = filePath;
			this.fileName = Path.GetFileName(filePath);
			this.progress = progress;
		}
		public Result(string fileName, string? filePath, int progress)
		{
			this.fileName = fileName ?? throw new ArgumentNullException(nameof(fileName));
			this.filePath = filePath;
			this.progress = progress;
		}
		public override bool Equals(object? obj)
		{
			return obj is Result result &&
				   fileName == result.fileName &&
				   filePath == result.filePath &&
				   progress == result.progress;
		}

		public string fileName { get; set; }
		public string? filePath { get; set; }
		public int progress { get; set; }
	}
	public enum EventType
	{
		UploadProgress,
		UploadComplete,
		UploadFailed,
		Error
	}

}