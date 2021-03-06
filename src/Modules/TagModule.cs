﻿using Discord;
using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.XPath;

namespace LossyBotRewrite
{
    [Name("Tag")]
    [Group("tag")]
    public class TagModule : ModuleBase<SocketCommandContext>
    {
        XDocument doc = XDocument.Load(Globals.path + "tags.xml");

        [Command("view")]
        [Summary("Gets a tag.\n`tag view [name]`")]
        public async Task TagView([Remainder] string name)
        {
            CheckServer(Context.Guild.Id);
            XElement tag = GetTag(name, Context.Guild.Id);

            if (tag == null)
            {
                await ReplyAsync("Tag does not exist.");
                return;
            }

            tag.SetAttributeValue("uses", int.Parse(tag.Attribute("uses").Value) + 1);
            await ReplyAsync($"{Context.User.Mention} {tag.Attribute("content").Value}");

            doc.Save(Globals.path + "tags.xml");
        }

        [Command("make")]
        [Summary("Creates a tag.\n`tag make [name] [content]`")]
        public async Task CreateTag(string name, [Remainder]string content)
        {
            CheckServer(Context.Guild.Id);

            if (content == null)
            {
                await ReplyAsync("Please add content for your tag!");
                return;
            }

            var serverElem = doc.Root.Elements("server").Where(elem => elem.Attribute("id").Value == Context.Guild.Id.ToString()).First();

            serverElem.Add(new XElement("tag", new XAttribute("name", name), 
                           new XAttribute("content", content), 
                           new XAttribute("uses", 0), 
                           new XAttribute("date", DateTime.Now.ToString("MM/dd/yyyy")), 
                           new XAttribute("author", Context.Message.Author.Id)));

            doc.Save(Globals.path + "tags.xml");
            await ReplyAsync($"Created tag '{name}'.");
        }
        
        [Command("delete")]
        [Summary("Deletes a tag.\n`tag delete [name]`")]
        public async Task DeleteTag([Remainder] string name)
        {
            CheckServer(Context.Guild.Id);

            XElement tag = GetTag(name, Context.Guild.Id);

            if (tag != null)
            {
                tag.Remove();
                doc.Save(Globals.path + "tags.xml");
                await ReplyAsync("Deleted tag successfully.");
            }
            else
            {
                await ReplyAsync("Tag does not exist.");
            }
        }

        [Command("info")]
        [Summary("Get info about a tag.\n`tag info [name]`")]
        public async Task TagInfo([Remainder] string name)
        {
            CheckServer(Context.Guild.Id);
            XElement tag = GetTag(name, Context.Guild.Id);

            if(tag == null)
            {
                await ReplyAsync("Tag does not exist.");
                return;
            }

            EmbedBuilder builder = new EmbedBuilder();

            builder.AddField("Name", name);
            builder.AddField("Author", Context.Client.GetUser(ulong.Parse(tag.Attribute("author").Value)).Mention);
            builder.AddField("Uses", tag.Attribute("uses").Value);
            builder.AddField("Date", tag.Attribute("date").Value);
            builder.WithColor(Color.Green);

            await ReplyAsync("", false, builder.Build());
        }

        [Command("search")]
        [Summary("Find a tag.\n`tag search [name]`")]
        public async Task TagSearch([Remainder] string name)
        {
            var serverElem = doc.Root.Elements("server").Where(elem => elem.Attribute("id").Value == Context.Guild.Id.ToString()).First();

            var tags = from tag in serverElem.Descendants() select tag;

            string master = "";
            foreach (var tag in tags)
            {
                if (tag.Attribute("name").Value.ToLower().Contains(name.ToLower()))
                    master += tag.Attribute("name").Value + "\n";
                if (master.Length > 900)
                    break;
            }

            if(master == "")
            {
                await ReplyAsync("No results found");
            }
            EmbedBuilder builder = new EmbedBuilder();
            builder.AddField($"Search results for '{name}'", master);
            builder.WithColor(Color.Green);

            await ReplyAsync("", false, builder.Build());
        }

        [Command("random")]
        [Summary("Get a random tag.\n`tag random`")]
        public async Task TagRandom()
        {
            CheckServer(Context.Guild.Id);

            var serverElem = doc.Root.XPathSelectElement($"./server[@id='{Context.Guild.Id}']");

            var tags = from tag in serverElem.Descendants() select tag;
            if (tags.Any())
            {
                Random random = new Random();
                var result = tags.ElementAt(random.Next(tags.Count()));
                await ReplyAsync(result.Attribute("content").Value);
                return;
            }
            await ReplyAsync("No tags available.");
        }

        private XElement GetTag(string name, ulong serverId)
        {
            var tag = doc.Root.XPathSelectElement($"./server[@id='{serverId}']/tag[@name='{name}']");

            return tag;
        }

        //If a server doesn't exist create it
        private void CheckServer(ulong serverId)
        {
            var server = doc.Root.XPathSelectElement($"./server[@id='{serverId}']");

            if (server == null)
            {
                doc.Root.Add(new XElement("server", new XAttribute("id", serverId)));
                doc.Save(Globals.path + "tags.xml");
            }
        }
    }
}
