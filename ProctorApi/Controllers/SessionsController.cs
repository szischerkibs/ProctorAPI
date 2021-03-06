﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Description;
using ProctorApi.DTO;
using ProctorApi.Models;
using ProctorApi.Repositories;
using ProctorApi.ViewModels;

namespace ProctorApi.Controllers
{
    [Authorize]
    public class SessionsController : ApiController
    {
        private ProctorContext db = new ProctorContext();
        private SessionRepository _sessionRepository;
        private RoomRepository _roomRepository;
        private UserCheckInRepository _userCheckInRepository;
        private string connStr = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;

        public SessionsController()
        {
            _sessionRepository = new SessionRepository();
            _roomRepository = new RoomRepository();
            _userCheckInRepository = new UserCheckInRepository();
        }

        // GET: api/Sessions
        public IList<SessionDto> GetSessions()
        {
            //var sessions =  db.Sessions
            //    .Include("Rooms")
            //    .ToList();
            //return sessions;
            return _sessionRepository.getSessions();
            
        }

        // GET: api/Sessions/5
        [ResponseType(typeof(SessionDto))]
        public IHttpActionResult GetSession(int id)
        {
            var session = _sessionRepository.getSessionById(id);
            //var session = db.Sessions.FirstOrDefault(s => s.Id == id);

            return Ok(session);
        }

        // PUT: api/Sessions/5
        [ResponseType(typeof(void))]
        public IHttpActionResult PutSession(int id, Session session)
        {
            if (id != session.Id)
            {
                return BadRequest();
            }

            var sessionToUpdate = db.Sessions
                .FirstOrDefault(s => s.Id == id);

            sessionToUpdate.Title = session.Title;
            sessionToUpdate.Abstract = session.Abstract;
            sessionToUpdate.SessionStartTime = session.SessionStartTime;
            sessionToUpdate.SessionEndTime = session.SessionEndTime;            
            sessionToUpdate.VolunteersRequired = session.VolunteersRequired;

            sessionToUpdate.ActualSessionStartTime = session.ActualSessionStartTime;
            sessionToUpdate.ActualSessionEndTime = session.ActualSessionEndTime;
            sessionToUpdate.Attendees10 = session.Attendees10;
            sessionToUpdate.Attendees50 = session.Attendees50;
            sessionToUpdate.Notes = session.Notes;
            

            db.SaveChanges();

            var rooms = db.Rooms.Where(r => r.SessionId == id).ToList();

            var deletedRooms =
                rooms.Where(x => !session.Rooms.Any(v => v.Name == x.Name)).ToList();
            var addedRooms =
                session.Rooms.Where(x => !rooms.Any(v => v.Name == x.Name)).ToList();

            //Do not need an update since there is only 1 field
            addedRooms.ForEach(x => _roomRepository.AddRoomToSession(x.Name, id) );
            deletedRooms.ForEach(x => _roomRepository.DeleteRoomFromSession(x.Name, id));

            //User Check Ins
            var userCheckIns = db.UserCheckIns.Where(r => r.SessionId == id).ToList();

            var deleteduserCheckIns =
                userCheckIns.Where(x => !session.ProctorCheckIns.Any(v => v.UserId == x.UserId)).ToList();
            var addeduserCheckIns =
                session.ProctorCheckIns.Where(x => !userCheckIns.Any(v => v.UserId == x.UserId)).ToList();

            

            //Do not need an update since there is only 1 field
            addeduserCheckIns.ForEach(x => _userCheckInRepository.AddUserCheckInToSession(x.UserId, x.CheckInTime, id));
            deleteduserCheckIns.ForEach(x => _userCheckInRepository.DeleteUserCheckInFromSession(x.UserId, id));

            //foreach (var checkIn in session.ProctorCheckIns)
            //{
            //    var foundCheckIn = userCheckIns.FirstOrDefault(c => c.UserId == checkIn.UserId);
            //    foundCheckIn.CheckInTime = checkIn.CheckInTime;
            //    db.SaveChanges();
            //}

            return StatusCode(HttpStatusCode.NoContent);
        }
        

        // POST: api/Sessions
        [ResponseType(typeof(Session))]
        public IHttpActionResult PostSession(Session session)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            db.Sessions.Add(session);
            db.SaveChanges();

            return CreatedAtRoute("DefaultApi", new { id = session.Id }, session);
        }

        // DELETE: api/Sessions/5
        [ResponseType(typeof(Session))]
        public IHttpActionResult DeleteSession(int id)
        {
            Session session = db.Sessions.Find(id);
            if (session == null)
            {
                return NotFound();
            }

            db.Sessions.Remove(session);
            db.SaveChanges();

            return Ok(session);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }

        private bool SessionExists(int id)
        {
            return db.Sessions.Count(e => e.Id == id) > 0;
        }

        // GET api/<controller>/GetUsersForRole/5
        [Route("api/Sessions/ImportFromFeed")]
        [HttpGet]
        public IHttpActionResult ImportFromFeed()
        {
            _sessionRepository.ImportFromFeed();
            return Ok();
        }

        // PUT api/<controller>/AutoAssign
        [Route("api/Sessions/AutoAssign")]
        [HttpPut]
        public IHttpActionResult AutoAssign()
        {
            _sessionRepository.AutoAssign();
            return Ok();
        }

        // GET api/<controller>/GetUsersForRole/5
        [Route("api/Sessions/GetSessionsPerUser")]
        [HttpGet]
        public List<UserDto> GetSessionsPerUser()
        {
            return _sessionRepository.GetSessionsPerUser();
            
        }

        // GET api/<controller>/GetUserSchedule/5
        [Route("api/Sessions/GetUserSchedule")]
        [HttpGet]
        public UserDto GetSessionsForUser(string userId)
        {
            return _sessionRepository.GetSessionsForUser(userId);

        }

        // GET api/<controller>/AddUserToSession/5
        [Route("api/Sessions/AddUserToSession")]
        [HttpPost]
        public IHttpActionResult AddUserToSession(string userId, int sessionId)
        {
            var sql = @"INSERT INTO dbo.SessionUsers
                            (
                                Session_Id
                              , User_Id
                            )
                            VALUES
                            (
                                @SessionId
	                            , @UserId
                            )";

            using (var conn = new SqlConnection(connStr))
            {
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@SessionId", sessionId);
                    cmd.Parameters.AddWithValue("@UserId", userId);

                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
            }

            //    var session = db.Sessions.FirstOrDefault(s => s.Id == sessionId);
            //var user = db.Users.FirstOrDefault(u => u.Id == userId);

            //session.Assignees.Add(user);
            //db.SaveChanges();

            return Ok();
        }

        // GET api/<controller>/RemoveUserFromSession
        [Route("api/Sessions/RemoveUserFromSession")]
        [HttpDelete]
        public IHttpActionResult RemoveUserFromSession(string userId, int sessionId)
        {
            var sql = @"DELETE dbo.SessionUsers WHERE Session_Id = @SessionId AND User_Id = @UserId";

            using (var conn = new SqlConnection(connStr))
            {
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@SessionId", sessionId);
                    cmd.Parameters.AddWithValue("@UserId", userId);

                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
            }

            //var session = db.Sessions.FirstOrDefault(s => s.Id == sessionId);
            //var user = db.Users.FirstOrDefault(u => u.Id == userId);

            //session.Assignees.Remove(user);
            //db.SaveChanges();

            return Ok();
        }

        // GET api/<controller>/Results
        [Route("api/Sessions/Results")]
        [HttpGet]
        public List<SessionResult> GetSessionResults()
        {
            return _sessionRepository.GetSessionResults();

        }

    }
}