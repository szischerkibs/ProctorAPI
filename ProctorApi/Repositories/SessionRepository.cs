﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ProctorApi.Models;
using ProctorApi.ViewModels;

namespace ProctorApi.Repositories
{
    public class SessionRepository
    {
        private string _speakerFeed;
        private string _sessionFeed;
        private readonly ProctorContext _context;

        public SessionRepository()
        {
            _speakerFeed = ConfigurationManager.AppSettings["SpeakerFeed"];
            _sessionFeed = ConfigurationManager.AppSettings["SessionFeed"];
            _context = new ProctorContext();
        }

        public void ImportFromFeed()
        {
            ImportSpeakers();
            ImportSessions();            
        }

        private void ImportSpeakers()
        {
            using (var webClient = new System.Net.WebClient())
            {
                var json = webClient.DownloadString(_speakerFeed);
                // Now parse with JSON.Net
                List<SpeakerImport> speakerImport = JsonConvert.DeserializeObject<List<SpeakerImport>>(json);

                foreach (SpeakerImport speaker in speakerImport)
                {
                    AddUpdateSpeaker(speaker);
                }
            }
        }

        private void AddUpdateSpeaker(SpeakerImport speaker)
        {
            var spkr = _context.Speakers.FirstOrDefault(s => s.Id == speaker.Id);
            if (spkr == null)
            {
                var newSpeaker = new Models.Speaker();

                newSpeaker.Id = speaker.Id;
                newSpeaker.FirstName = speaker.FirstName;
                newSpeaker.LastName = speaker.LastName;
                newSpeaker.Biography = speaker.Biography;
                newSpeaker.GravatarUrl = speaker.GravatarUrl;
                newSpeaker.TwitterLink = speaker.TwitterLink;
                newSpeaker.GitHubLink = speaker.GitHubLink;
                newSpeaker.LinkedInProfile = speaker.LinkedInProfile;
                newSpeaker.BlogUrl = speaker.BlogUrl;

                _context.Speakers.Add(newSpeaker);
                _context.SaveChanges();
            }
        }

        private void ImportSessions()
        {
            using (var webClient = new System.Net.WebClient())
            {
                var json = webClient.DownloadString(_sessionFeed);
                // Now parse with JSON.Net
                List<SessionImport> sessionImport = JsonConvert.DeserializeObject<List<SessionImport>>(json);

                foreach (SessionImport session in sessionImport)
                {
                    AddUpdateSession(session);
                }
            }
        }

        private void AddUpdateSession(SessionImport session)
        {
            var sn = _context.Sessions.FirstOrDefault(s => s.Id == session.Id);
            if (sn == null)
            {
                var newSession = new Session();
                newSession.Id = session.Id;
                newSession.SessionTime = DateTime.MinValue;
                newSession.SessionStartTime = session.SessionStartTime;
                newSession.SessionEndTime = session.SessionEndTime;
                newSession.Rooms = AddRooms(session.Rooms, session.Id);
                newSession.Title = session.Title;
                newSession.Abstract = session.Abstract;
                newSession.Tags = AddTags(session.Tags, session.Id);
                newSession.Category = session.Category;
                newSession.SessionType = session.SessionType;
                newSession.Speakers = AddSpeakers(session.Speakers);

                _context.Sessions.Add(newSession);
            }
            else
            {
                sn.SessionStartTime = session.SessionStartTime;
                sn.SessionEndTime = session.SessionEndTime;
                sn.Rooms = AddRooms(session.Rooms, session.Id);
                sn.Title = session.Title;
                sn.Abstract = session.Abstract;
                sn.Tags = AddTags(session.Tags, session.Id);
                sn.Category = session.Category;
                sn.SessionType = session.SessionType;
                sn.Speakers = AddSpeakers(session.Speakers);

            }
            _context.SaveChanges();           
        }

        private List<Models.Speaker> AddSpeakers(List<ViewModels.Speaker> speakers)
        {
            List<Models.Speaker> speakerList = new List<Models.Speaker>();

            if (speakers == null) { return speakerList; }

            foreach (ViewModels.Speaker speaker in speakers)
            {
                var spkr = _context.Speakers.FirstOrDefault(s => s.Id == speaker.Id);
                if (spkr == null)
                {
                    speakerList.Add(new Models.Speaker()
                    {
                        Id = speaker.Id,
                        FirstName = speaker.FirstName,
                        LastName = speaker.LastName,
                        GravatarUrl = speaker.GravatarUrl
                    });
                }
                else
                {
                    speakerList.Add(spkr);
                }
            }

            return speakerList;
        }

        private List<Tag> AddTags(List<object> tags, int sessionId)
        {
            List<Tag> tagsList = new List<Tag>();

            if (tags == null) { return tagsList; }

            foreach (string tag in tags)
            {
                tagsList.Add(new Tag()
                {
                    Name = tag,
                    SessionId = sessionId
                });
            }

            return tagsList;
        }

        private List<Room> AddRooms(List<string> rooms, int sessionId)
        {
            List<Room> roomsList = new List<Room>();

            if (rooms == null) { return roomsList; }

            foreach(string room in rooms)
            {
                roomsList.Add(new Room() {
                    Name = room,
                    SessionId = sessionId
                });
            }

            return roomsList;

        }
    }
}