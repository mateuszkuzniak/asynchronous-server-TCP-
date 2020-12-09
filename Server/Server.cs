﻿using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using DatabaseLibrary;
using MessageLibrary;
using Exceptions;

namespace ServerLibrary
{
    public abstract class Server<T> where T : CommunicationProtocol, new()
    {
        #region FIELDS;
        IPAddress iPAddress;
        int port;
        int bufferSize = 1024;
        protected bool running;
        protected delegate void TransmissionDataDelegate(NetworkStream stream);
        bool validIp = true;

        #endregion
        public User _usersDatabase;
        public FileDb _filesDatabase;

        #region PROPERTIES
        public IPAddress IPAddress { get => iPAddress; set { if (!running) iPAddress = value; else throw new Exception("The IP address cannot be changed while the server is running"); } }

        public int Port
        {
            get => port; set
            {
                int temp = port;
                if (!running) port = value; else throw new Exception("The port cannot be changed while the server is running");
                if (!checkPort())
                {
                    port = temp;
                    throw new ArgumentOutOfRangeException("Port out of range!\nPort range: 1024-49151.\nPort set to 8000");
                }
            }
        }

        public int Buffer_size
        {
            get => bufferSize; set
            {
                if (value < 0 || value > 1024 * 102 * 64) throw new Exception("Wrong size packed ");
                if (!running) bufferSize = value; else throw new Exception("The packet size cannot be changed while the server is running");
            }
        }

        protected TcpListener TcpListener { get; set; }

        #endregion

        #region CONSTRUCTORS
        public Server(IPAddress IP, int port)
        {
            _usersDatabase = new User("users", "users");
            _filesDatabase = new FileDb("users", "files");
            running = false;
            IPAddress = IP;
            try
            {
                Port = port;
            }
            catch (ArgumentOutOfRangeException ex)
            {
                Port = 8000;
                Console.WriteLine(ex.Message);
            }


        }
        #endregion

        #region FUNCTIONS
        bool checkPort()
        {
            if (port < 1024 || port > 49151) return false;
            return true;
        }

        protected void StartListening()
        {
            try
            {
                TcpListener = new TcpListener(IPAddress, Port);
                TcpListener.Start();
            }
            catch (SocketException ex)
            {
                Console.WriteLine(ex.Message);
                validIp = false;
            }
        }

        protected abstract void AcceptClient();

        protected string ReadMessage(NetworkStream stream)
        {
            string message;
            int size;
            byte[] reciveBuffer = new byte[bufferSize];

            size = stream.Read(reciveBuffer, 0, reciveBuffer.Length);
            if (reciveBuffer[0] == 13 && reciveBuffer[1] == 10)
                size = stream.Read(reciveBuffer, 0, reciveBuffer.Length);
            message = Encoding.UTF8.GetString(reciveBuffer, 0, size);

            return message.Trim(new char[] { '\r', '\n', '\0' });
        }

        protected void BeginDataTransmission(NetworkStream stream)
        {
            Account user = new Account();
            CommunicationProtocol protocol = new T();
            protocol.SetDatabaseUser(_usersDatabase);
            protocol.SetDatabaseFile(_filesDatabase);
            string message = "";
            string response;
            byte[] buffer;



            while (true)
            {
                message = "";
                try
                {
                    message = ReadMessage(stream);
                    response = protocol.GenerateResponse(message);
                    buffer = ASCIIEncoding.UTF8.GetBytes(response);
                    stream.Write(buffer, 0, buffer.Length);
                    buffer = new byte[Buffer_size];
                }
                catch (IOException)
                {
                    Console.WriteLine(ServerMessage.CloseConnection(user.Login));
                    if (protocol.GetUserStatus())
                        _usersDatabase.UpdateLoginStatus(protocol.GetUser());
                    break;
                }

            }
        }

        public void ServerConsole()
        {
            while (true)
            {
                string cmd = Console.ReadLine().ToLower();
                if (cmd == "shutdown" || !validIp)
                    throw new CloseServerException();
                else if (cmd == "show users")
                    Console.WriteLine(_usersDatabase.GetAllLogedUser());
            }
        }

        public string GetAllLoggedUsers()
        {
            return _usersDatabase.GetAllLogedUser();
        }

        public abstract void Start();
        public void Stop()
        {
            _usersDatabase.MakeLogOut(_usersDatabase.TableName);
            TcpListener.Stop();
        }
        #endregion

    }
}