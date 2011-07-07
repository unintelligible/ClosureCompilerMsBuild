using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Net;

namespace ClosureCompilerMsBuild
{
    /// <summary>
    /// largely based on http://closurecompiler.codeplex.com/SourceControl/changeset/view/42364#913323
    /// with better error handling and formatted VS output
    /// </summary>
    public class ClosureHostedCompiler : ClosureCompilerBase
    {
        private String _apiUrl = "http://closure-compiler.appspot.com/compile";
        public String ApiUrl
        {
            get { return _apiUrl; }
            set { _apiUrl = value; }
        }

        public override bool Execute()
        {
            Log.LogMessage(String.Format("Found {0} javascript files to compile using the Closure compiler", SourceFiles.Count()));
            var errorCount = 0;
            foreach (var t in SourceFiles)
            {
                try
                {
                    Log.LogMessage(String.Concat("Compiling JS file: ", t.ItemSpec), null);

                    var source = File.ReadAllText(t.ItemSpec);
                    var thisErrorCount = 0;

                    if (IsShowErrors)
                    {
                        thisErrorCount = GetErrors(source, t.ItemSpec);
                        errorCount += thisErrorCount;
                    }

                    if (IsShowWarnings)
                    {
                        GetWarnings(source, t.ItemSpec);
                    }

                    if (IsWriteOutput)
                    {
                        var output = GetOutput(source);
                        if (!MaybeWriteOutput(t.ItemSpec, output))
                            return false;
                    }
                }
                catch (Exception ex)
                {
                    Log.LogErrorFromException(ex);
                    return false;
                }
            }

            return errorCount == 0;
        }

        private string GetOutput(string source)
        {
            var xmlResponse = SendFileToCompiler(source, "compiled_code");
            if (!HandleServerErrors(xmlResponse))
                throw new HostedCompilerException("Server error");
            return xmlResponse.SelectSingleNode("//compiledCode").InnerText;
        }

        private void GetWarnings(string source, string filename)
        {
            var xmlResponse = SendFileToCompiler(source, "warnings");
            if (!HandleServerErrors(xmlResponse))
                throw new HostedCompilerException("Server error");
            var jsWarningsNode = xmlResponse.SelectSingleNode("//warnings");
            if (jsWarningsNode != null)
            {
                foreach (XmlNode n in jsWarningsNode.ChildNodes)
                {
                    var lineNo = int.Parse(n.Attributes["lineno"].Value);
                    var charNo = int.Parse(n.Attributes["charno"].Value);
                    LogWarning(filename, lineNo, charNo, n.Attributes["type"].Value);
                }
            }
        }

        private int GetErrors(string source, string filename)
        {
            var xmlResponse = SendFileToCompiler(source, "errors");
            if (!HandleServerErrors(xmlResponse))
                throw new HostedCompilerException("Server error");
            var jsErrorsNode = xmlResponse.SelectSingleNode("//errors");
            var thisErrorCount = 0;
            if (jsErrorsNode != null)
            {
                foreach (XmlNode n in jsErrorsNode.ChildNodes)
                {
                    var lineNo = int.Parse(n.Attributes["lineno"].Value);
                    var charNo = int.Parse(n.Attributes["charno"].Value);
                    LogError(filename, lineNo, charNo, n.Attributes["type"].Value);
                    thisErrorCount++;
                }
            }
            return thisErrorCount;
        }

        private bool HandleServerErrors(XmlDocument xmlResponse)
        {
            var serverErrorsNode = xmlResponse.SelectSingleNode("//serverErrors");
            if (serverErrorsNode != null)
            {
                foreach (XmlNode n in serverErrorsNode.ChildNodes)
                {
                    var errorCode = int.Parse(n.Attributes["code"].Value);
                    var errorMessage = n.InnerText;
                    Log.LogError("Closure Compiler Server Error (code {0}): {1}", errorCode, errorMessage);
                }
                return false;
            }
            return true;
        }

        private XmlDocument SendFileToCompiler(String source, string outputInfo)
        {

            var postData = new Dictionary<String, String>();
            postData.Add("js_code", source);
            postData.Add("compilation_level", CompilationLevel);
            postData.Add("output_format", "xml");
            postData.Add("output_info", outputInfo);

            var sb = new StringBuilder();
            foreach (var e in postData)
            {
                if (sb.Length > 0)
                {
                    sb.Append("&");
                }

                sb.Append(e.Key);
                sb.Append("=");
                sb.Append(System.Web.HttpUtility.UrlEncode(e.Value));
            }

            var data = Encoding.UTF8.GetBytes(sb.ToString());
            var request = (HttpWebRequest)WebRequest.Create(ApiUrl);
            request.ContentLength = data.Length;
            request.ContentType = "application/x-www-form-urlencoded";
            request.Method = "POST";

            var requestStream = request.GetRequestStream();
            requestStream.Write(data, 0, data.Length);

            var response = (HttpWebResponse)request.GetResponse();
            var dataStream = response.GetResponseStream();

            var reader = XmlReader.Create(dataStream);
            var xml = new XmlDocument();
            xml.Load(reader);

            requestStream.Close();
            reader.Close();
            dataStream.Close();
            response.Close();

            return xml;
        }
    }

    internal class HostedCompilerException : Exception
    {
        public HostedCompilerException(string serverError)
            : base(serverError)
        {
            
        }
    }
}
