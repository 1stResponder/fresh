using Fresh.Global.IconHelpers;
using System;
using System.Collections.Generic;
using System.Web;
using System.IO;
using System.Xml.Serialization;
using System.Xml;

namespace Fresh.Global
{
  public static class IconConfig
  {
    //TODO: put in the web config file
    static string IconFileName = System.Web.Hosting.HostingEnvironment.MapPath("~/App_Data/IconFiles.xml");

    static FreshIcons mIcons;

    public static FreshIcons Icons
    {
      get
      {
        if (mIcons == null)
        {
          try
          {
            XmlSerializer xs = new XmlSerializer(typeof(FreshIcons));
            using (FileStream fileStream = new FileStream(IconFileName, FileMode.Open))
            {
              mIcons = (FreshIcons)xs.Deserialize(fileStream);
            }
          }
          catch (Exception Ex)
          {
            DEUtilities.LogMessage(Ex.ToString(), DEUtilities.LogLevel.Error);
          }
        }

        return mIcons;
      }

      set
      {
        mIcons = value;

        if (mIcons != null)
        {

          XmlSerializer xs = new XmlSerializer(typeof(FreshIcons));
          using (TextWriter tw = new StreamWriter(IconFileName))
          {
            xs.Serialize(tw, mIcons);
          }
        }
      }

    }

    // parses sets from Xml file
    // calls buildGroup to handle groups and icons
    private static void buildSets(string file)
    {
      XmlReader reader = XmlReader.Create(file);
      reader.ReadStartElement("FreshIcons");

      while (reader.Read())
      {
        if (reader.NodeType == XmlNodeType.Element && reader.Name == "IconSet")
        {
          IconSet set = new IconSet();
          set.KindofSet = reader.GetAttribute(0);
          set.Groups = buildGroup(file, set.KindofSet);

          mIcons.Sets.Add(set);
        }
      }
    }

    // parses groups their associated files for given IconSet
    private static List<IconGroup> buildGroup(string file, string setname)
    {
      List<IconGroup> grouplist = new List<IconGroup>();

      XmlReader reader = XmlReader.Create(file);
      reader.ReadStartElement("FreshIcons");

      Boolean found = false;
      while(reader.Read())
      {
        // set reader location to this group's IconSet
        if (reader.Name == "IconSet" && reader.GetAttribute(0) == setname && reader.IsStartElement())
        {
          found = true;
          break;
        }    
              
        else reader.Skip(); // skip over children of current node
      }

      // proper IconSet found in file
      if (found)
      {
        // continue reading
        while (reader.Read())
        {
          if(reader.Name == "IconGroup")
          {
            /* Set KindofGroup */
            string KindofGroup = reader.GetAttribute(0);


            /* Set RootFolder */
            string RootFolder;
            // next element should be RootFolder, throw exception otherwise
            if (reader.Read())
            {
              if (reader.Name != "RootFolder") throw new XmlException("Invalid Xml format.");
              RootFolder = reader.ReadContentAsString();
            }
            else throw new XmlException("Invalid Xml format.");


            /* Set Filenames */
            List<string> Filenames = new List<string>();
            // next element should be Icons, throw exception otherwise
            if (reader.Read())
            {
              if (reader.Name != "Icons") throw new XmlException("Invalid Xml format.");
              
              // advance reader; in most cases will now point to first icon file; includes check for invalid file structure
              if (!reader.Read()) throw new XmlException("Invalid Xml format."); 
              while(reader.Name == "Icon")
              {
                Filenames.Add(reader.ReadContentAsString());
                if (!reader.Read()) throw new XmlException("Invalid Xml format.");
              }
            }
            else throw new XmlException("Invalid Xml format.");

            /* Add IconGroup to list */
            grouplist.Add(new IconGroup(KindofGroup, RootFolder, Filenames));
          }
        }
      }

      // did not find requested group - throw error
      else
      {
        throw new XmlException("Requested IconGroup not found.");
      }

      return grouplist;
    }

  }
}
