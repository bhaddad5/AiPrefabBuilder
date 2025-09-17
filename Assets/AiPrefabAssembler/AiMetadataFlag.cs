using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AiMetadataFlag : MonoBehaviour
{

	public string AiMetadataTitle = "";

	public string AiMetadataSummary = "";

	[TextArea(3, 10)]
	public string AiMetadataDescription = "";

	public List<string> AiMetadataTags = new List<string>();
}
