# Photo Directory Creator
Creates an HTML photo directory from a collection of JPEG photos with associated metadata.

## How to use
Note: The tool is based on having the right metadata set on your .jpg files. Metadata can be edited using Microsoft Windows Explorer by turning on the details pain (View tab -> Details Pane). In Windows you can also right-click a file, select Properties and then select the the "Details" tab. In either case, update the metadata and then click "Save."

Windows seems to have problems keeping the metadata in sync if the directory is on OneDrive. You may change a metadata field only to have Windows revert it to the previous value. If you want to use OneDrive as a backup location (a very good idea) then copy to a non-OneDrive folder, get your metadata right, and then copy back.

1. Collect a set of JPEG photos of people to include in your directory. Portrait orientation works best. Put all of the .jpg files in one folder.
2. Set the "Title" metadata field on each photo to the name of the individual.
3. Set the "Tag" (keyword) metadata on each photo to the individual's address. Use a one-line abbreviated address. For apartments, use the complex name and the apartment number (e.g. "Jamestown 25").
4. Invoke the Photo Directory command-line program providing the name of the folder containing the source images, the folder to receive the created directory, and the title of the directory.

**Sample Command Line**:
```
PhotoDirectory "c:\data\PhotoDirectorySource" "c:\data\PhotoDirectory" "The A Team"
```

The program will resize the photos to web resolution if necessary, copy them to the destination directory, and create an HTML file for the directory. The HTML includes appropriate CSS for hard page breaks.

To create a PDF file, open the directory file in a browser and print to PDF. That capability comes with most recent versions of windows.