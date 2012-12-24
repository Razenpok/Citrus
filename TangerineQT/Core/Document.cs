﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tangerine
{
	public delegate void DocumentEventHandler(Document document);

	public class Document
	{
		public static Document Null = new Document(readOnly: true);

		public static Document Instance = Null;
		
		public Lime.Node RootNode { get; private set; }

		public DocumentSettings Settings = new DocumentSettings();

		public bool ReadOnly;

		public static event DocumentEventHandler Changed;

		public Document(bool readOnly)
		{
			Instance = this;
			this.ReadOnly = readOnly;
			RootNode = new Lime.Widget();
			RootNode.Guid = Guid.NewGuid();
		}

		public void OnChanged()
		{
			if (Changed != null) {
				Changed(this);
			}
		}

		public void AddSomeNodes()
		{
			for (int i = 0; i < 20; i++) {
				var frame = new Lime.Frame();
				frame.Guid = Guid.NewGuid();
				frame.Id = string.Format("Image {0:00}", i);

				var ani = frame.Animators["Position"];
				ani.Add(0, new Lime.Vector2(0, 0));
				ani.Add(10, new Lime.Vector2(100, 0));
				ani.Add(12, new Lime.Vector2(100, 0));
				ani.Add(16, new Lime.Vector2(100, 0));
				ani.Add(19, new Lime.Vector2(100, 0));
				ani.Add(30, new Lime.Vector2(200, 0));

				ani = frame.Animators["Scale"];
				ani.Add(5, new Lime.Vector2(0, 0));
				ani.Add(15, new Lime.Vector2(100, 0));
				//ani.Add(20, new Lime.Vector2(100, 0));
				//ani.Add(30, new Lime.Vector2(200, 0));

				ani = frame.Animators["Size"];
				ani.Add(0, new Lime.Vector2(0, 0));
				ani.Add(50, new Lime.Vector2(50, 0));
				ani.Add(100, new Lime.Vector2(100, 0));
				//ani.Add(20, new Lime.Vector2(100, 0));
				//ani.Add(30, new Lime.Vector2(200, 0));

				ani = frame.Animators["Rotation"];
				ani.Add(15, 0f);
				ani.Add(60, 1f);

				//ani.Add(50, 0);

				RootNode.AddNode(frame);
			}
		}
	}
}
