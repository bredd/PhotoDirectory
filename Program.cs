using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;
using WinShell;
using Interop;
using FileMeta;

namespace PhotoDirectory
{
    class Program
    {
        static PropertyKey s_pkTitle = new PropertyKey("F29F85E0-4FF9-1068-AB91-08002B27B3D9", 2);
        static PropertyKey s_pkTags = new PropertyKey("F29F85E0-4FF9-1068-AB91-08002B27B3D9", 5);

        const string c_directoryFilename = "PhotoDirectory.html";
        const int c_photoResolutionWidth = 710;
        const int c_linesPerCol = 52;

        static void Main(string[] args)
        {
            try
            {
                if (args.Length < 2)
                {
                    throw new Exception("Syntax: PhotoDirectory <source folder> <destination folder> [Title]");
                }

                string srcDir = Path.GetFullPath(args[0]);
                if (!Directory.Exists(srcDir))
                {
                    throw new Exception("Directory doesn't exist: " + srcDir);
                }

                string dstDir = Path.GetFullPath(args[1]);
                if (!Directory.Exists(dstDir))
                {
                    Directory.CreateDirectory(dstDir);
                }
                else
                {
                    // TODO: Give a warning before deleting existing files.
                    foreach(var filename in Directory.GetFiles(dstDir))
                    {
                        File.Delete(filename);
                    }
                }

                var title = (args.Length > 2) ? args[2] : string.Empty;

                var photos = LoadPhotos(srcDir);

                GenerateDirectory(photos, dstDir, title);

                foreach(var photo in photos)
                {
                    Console.WriteLine($"{photo.Title}, {photo.Tags}, {photo.Filename}");
                }
            }
            catch (Exception err)
            {
                Console.WriteLine(err.ToString());
            }

            if (Debugger.IsAttached)
            {
                Console.WriteLine("Press any key to exit.");
                Console.ReadKey();
            }

        }

        static List<Photo> LoadPhotos(string folderName)
        {
            var photos = new List<Photo>();
            foreach (string filename in Directory.GetFiles(folderName, "*.jpg"))
            {
                using (var ps = PropertyStore.Open(filename))
                {
                    var photo = new Photo();
                    photo.Filename = filename;
                    photo.Title = (ps.GetValue(s_pkTitle) as string ?? string.Empty).Trim();
                    var tags = ps.GetValue(s_pkTags) as string[];
                    photo.Tags = ((tags != null) ? string.Join(" ", tags) : string.Empty).Trim();

                    photos.Add(photo);
                }
            }

            return photos;
        }

        static void GenerateDirectory(List<Photo> photos, string dstDir, string title)
        {
            string dstFilename = Path.Combine(dstDir, c_directoryFilename);
            using (var writer = new StreamWriter(dstFilename, false, new UTF8Encoding(false)))
            {
                int pageNum = 0;

                writer.Write(c_htmlStartDoc);
                writer.Write(c_htmlStartBody);

                // Photo Directory
                photos.Sort(CompareNames);

                using (var photoEnum = photos.GetEnumerator())
                {

                    bool havePhoto = photoEnum.MoveNext();

                    while (havePhoto)
                    {
                        ++pageNum;
                        writer.Write(c_htmlStartPage);
                        writer.Write(c_htmlPageHeader, title, pageNum);
                        for (int row = 0; row < 3 && havePhoto; ++row)
                        {
                            writer.Write(c_htmlStartRow);
                            for (int col = 0; col < 3 && havePhoto; ++col)
                            {
                                // Generate the destination path
                                string photoFilename = FilenameEncode(photoEnum.Current.Title) + ".jpg";
                                string photoPath = Path.Combine(dstDir, photoFilename);

                                Console.WriteLine(photoFilename);
                                ImageFile.ResizeAndRightImage(photoEnum.Current.Filename, photoPath, c_photoResolutionWidth, 0);

                                string name = HtmlEncode(photoEnum.Current.Title);
                                string apt = HtmlEncode(photoEnum.Current.Tags);
                                writer.Write(c_htmlPhoto, photoFilename,
                                    name, apt);
                                havePhoto = photoEnum.MoveNext();
                            }
                            writer.Write(c_htmlEndRow);
                        }
                        writer.Write(c_htmlEndPage);
                    }
                }

                // === Apartment Directory ===
                photos.Sort(CompareApartments);

                ++pageNum;
                writer.Write(c_htmlStartPage);
                writer.Write(c_htmlPageHeader, title, pageNum);

                using (var photoEnum = photos.GetEnumerator())
                {
                    int colRows = 0;
                    var havePhoto = photoEnum.MoveNext();

                    while (havePhoto)
                    {
                        // Accumulate one apartment full
                        string currentApartment = photoEnum.Current.Tags;
                        var apartment = new List<Photo>();
                        do
                        {
                            apartment.Add(photoEnum.Current);
                            havePhoto = photoEnum.MoveNext();
                        } while (havePhoto && string.Equals(currentApartment, photoEnum.Current.Tags, StringComparison.OrdinalIgnoreCase));

                        // End a column if needed
                        if (colRows > 0 && colRows + apartment.Count + 2 > c_linesPerCol)
                        {
                            writer.Write(c_htmlEndAptCol);
                            colRows = 0;
                        }

                        // Begin a column if needed
                        if (colRows == 0)
                        {
                            writer.Write(c_htmlStartAptCol);
                        }

                        // Write the apartment out
                        writer.Write(c_htmlStartApartment, currentApartment);
                        foreach (var photo in apartment)
                        {
                            writer.Write(c_htmlName, photo.Title);
                        }
                        writer.Write(c_htmlEndApartment);
                        colRows += apartment.Count + 2;
                    }

                    if (colRows != 0)
                    {
                        writer.Write(c_htmlEndAptCol);
                    }
                }

                writer.Write(c_htmlEndPage);

                // === By First Name ===
                /*
                photos.Sort(CompareNames);

                ++pageNum;
                colRows = 0;
                writer.Write(c_htmlStartPage);
                writer.Write(c_htmlPageHeader, title, pageNum);

                foreach(var photo in photos)
                {
                    if (colRows >= c_linesPerCol)
                    {
                        writer.Write(c_htmlEndListCol);
                        colRows = 0;
                    }
                    if (colRows == 0)
                    {
                        writer.Write(c_htmlStartListCol);
                    }
                    writer.Write(c_htmlListEntry, photo.Title, photo.Tags);
                    ++colRows;
                }

                if (colRows > 0)
                {
                    writer.Write(c_htmlEndListCol);
                }

                writer.Write(c_htmlEndPage);

                // Finish it off.
                writer.Write(c_htmlEndBody);
                writer.Write(c_htmlEndDoc);
                */
            }
        }

        static string HtmlEncode(string value)
        {
            if (string.IsNullOrEmpty(value)) return "&nbsp;";
            return System.Web.HttpUtility.HtmlEncode(value);
        }

        static int CompareNames(Photo a, Photo b)
        {
            return string.Compare(a.Title, b.Title);
        }

        static int CompareApartments(Photo a, Photo b)
        {
            var c = string.Compare(a.Tags, b.Tags);
            if (c != 0) return c;
            return string.Compare(a.Title, b.Title);
        }

        static int CompareLastNames(Photo a, Photo b)
        {
            string sa = a.Title;
            string sb = b.Title;
            int ia = sa.LastIndexOf(' ')+1;
            int ib = sb.LastIndexOf(' ')+1;
            int len = Math.Max(sa.Length - ia, sb.Length - ib);

            var c = string.Compare(sa, ia, sb, ib, len, true);
            if (c != 0) return c;
            return string.Compare(sa, sb, true);
        }

        static string FilenameEncode(string value)
        {
            var sb = new StringBuilder();
            foreach(var c in value)
            {
                if (char.IsLetterOrDigit(c))
                {
                    sb.Append(c);
                }
            }
            return sb.ToString();
        }

        const string c_htmlStartDoc =
@"<!DOCTYPE html>
<html lang='en-us'>

<head>
    <style>

body {
    font-family: Verdana, Geneva, Tahoma, sans-serif;
    font-size: 11pt;
}

figure {
    display: inline-block;
    margin: 0.02in;
}

.page {
    border-top: solid black 1px;
    page-break-after: always;
}

.row {
    margin-bottom: 1em;
}

.fig-name {
    text-align: center;
    font-weight: bold;
}

.fig-apt {
    text-align: center;
}

.crop-box {
    border: black solid 1px;
    border-radius: 5px;
    height: 2.6in;
    width: 2.4in;
    overflow: hidden;
}

.crop-box img {
    width: 2.4in;
}

.aptcol {
    display: inline-block;
    vertical-align: top;
    width: 1.8in;
}

.apartment {
    margin-bottom: 1em;
}

.aptname {
    font-weight: bold;
}

.listcol {
    display: inline-block;
    vertical-align: top;
    width: 2.4in;
}

.list-name {
    display: inline-block;
    width: 1.8in;
}

.list-apt {
    display: inline-block;
    width: 0.6in;
    font-size: 0.5em;
}

.pageheader {
    text-align: center;
    padding-bottom: 1em;
}

.pagetitle {
    font-weight: bold;
}

.pagenumber {
    float: right;
}

    </style>
</head >


";

        const string c_htmlEndDoc =
@"</html>
";

        const string c_htmlStartBody =
@"<body>
";

        const string c_htmlEndBody =
@"</body>
";

        const string c_htmlStartPage =
@"    <div class='page'>
";

        const string c_htmlEndPage =
@"    </div> <!-- page -->
";

        const string c_htmlPageHeader =
@"        <div class='pageheader'><span class='pagetitle'>{0}</span> <span class='pagenumber'>{1}</span></div>
";

        const string c_htmlStartRow =
@"        <div class='row'>
";

        const string c_htmlEndRow =
@"        </div> <!-- row -->
";

        const string c_htmlPhoto =
@"            <figure>
                  <div class='crop-box'>
                      <img src='{0}'/>
                  </div>
                  <div class='fig-name'>{1}</div>
                  <div class='fig-apt'>{2}</div>
              </figure>
";

        const string c_htmlStartAptCol =
@"        <div class='aptcol'>
";

        const string c_htmlEndAptCol =
@"        </div>
";

        const string c_htmlStartApartment =
@"        <div class='apartment'>
             <div class='aptname'>{0}</div>
";

        const string c_htmlEndApartment =
@"        </div>
";

        const string c_htmlStartListCol =
@"        <div class='listcol'>
";

        const string c_htmlEndListCol =
@"        </div>
";

        const string c_htmlName =
@"        <div class='name'>{0}</div>
";

        const string c_htmlListEntry =
@"        <div><span class='list-name'>{0}</span><span class='list-apt'>{1}</span></div>
";

    }

    class Photo
    {
        public string Filename;
        public string Title;
        public string Tags;
    }
}
