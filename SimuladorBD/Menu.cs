﻿using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

namespace SimuladorBD {
    enum CurrentState {
        databaseUnselected,
        databaseSelected
    }
    internal class Menu {
        private Database CurrentDatabase = null;
        /// <summary>
        /// Gets or sets current action user is doing
        /// </summary>
        private CurrentState CurrentAction { get; set; } = CurrentState.databaseUnselected;
        /// <summary>
        /// Gets the root path of the program executed
        /// </summary>
        public string RootPath {
            get; private set;
        }
        /// <summary>
        /// Gets the Array of all the directories inside the current program, also called Databases
        /// </summary>
        public List<Database> Directories {
            get; private set;
        }
        public Menu(string currentPath) {
            Console.Title = "Simulador de Base de Datos | By Jonathan Mojica | Root";
            this.RootPath = currentPath;
            LoadDatabases();
        }
        /// <summary>
        /// Clears the console and awaits for a query.
        /// </summary>
        public void Show() {
            Console.Clear();
            string query = null;
            int currentLine = 0;
            bool spaceOnce = false;

            while (true) {
                char keyPressed = Console.ReadKey().KeyChar;

                // Detect double space and skip it.
                if (( keyPressed == '\u0020' || keyPressed == '\t' ) && spaceOnce)
                    continue;
                // Detect single space or reset spaceOne flag
                spaceOnce = keyPressed == '\u0020' || keyPressed == '\t';

                if (keyPressed == ';')
                    break;
                if (keyPressed == '\r') {
                    currentLine++;
                    if (!spaceOnce) {
                        query += '\u0020';
                        spaceOnce = true;
                    }
                    Console.SetCursorPosition(0, currentLine);
                }
                else if (keyPressed == '\b' && query.Length > 0)
                    query = query.Substring(0, query.Length - 1);
                else
                    query += keyPressed;
            }
            Console.WriteLine();

            try {
                AnalyzeQuery(query);
            }
            catch (ExitProgramException) { }
            catch (ReturnToMainException) {
                this.CurrentAction = CurrentState.databaseUnselected;
                Console.Title = "Simulador de Base de Datos | By Jonathan Mojica | Root";
                Show();
            }
            catch (IndexOutOfRangeException) {
                Console.Write(new InstructionNotFoundException().Message);
                Console.ReadKey();
                Show();
            }
            catch (HandledException ex) {
                Console.Write(ex.Message);
                Console.ReadKey();
                Show();
            }
            catch (Exception ex) {
                Console.Write(ex.ToString());
                Console.ReadKey();
                Show();
            }
        }
        /// <summary>
        /// Analyzes the query entered and decides what to do
        /// </summary>
        /// <param name="query">Query from the user</param>
        void AnalyzeQuery(string query) {
            string formatedQuery = query.ToUpper().Trim();
            string[] queryElements = formatedQuery.Split('\u0020');
            if (queryElements[0] == "SALIR")
                throw new ExitProgramException();
            if (queryElements[0] == "RETROCEDE")
                throw new ReturnToMainException();
            string queryToAnalyze = queryElements[0] + "\u0020" + queryElements[1];

            if (this.CurrentAction == CurrentState.databaseUnselected)
                switch (queryToAnalyze) {
                    case "CREA BASE":
                        CreateDatabase(queryElements[2]);
                        break;

                    case "BORRA BASE":
                        DeleteDatabase(queryElements[2]);
                        break;

                    case "MUESTRA BASES":
                        ShowDatabases();
                        break;

                    case "USA BASE":
                        UseDatabase(queryElements[2]);
                        break;

                    default:
                        throw new InstructionNotFoundException();
                }
            else
                if (queryElements[0] == "LISTA")
                EvaluateList(query);
            else
                switch (queryToAnalyze) {
                    case "CREA TABLA":
                        this.CurrentDatabase.CreateTable(queryElements[2], query);
                        break;

                    case "MUESTRA TABLAS":
                        this.CurrentDatabase.ShowTables();
                        break;

                    case "BORRA TABLA":
                        this.CurrentDatabase.DeleteTable(queryElements[2]);
                        break;

                    case "BORRA CAMPO":
                        this.CurrentDatabase.DeleteField(query, queryElements[2].Replace(",", "\u0020").Trim());
                        break;

                    case "AGREGA CAMPO":
                        this.CurrentDatabase.AddField(query, queryElements[2].Replace(",", "\u0020").Trim());
                        break;

                    case "INSERTA EN":
                        this.CurrentDatabase.Insert(query, queryElements[2].Replace(",", "\u0020").Trim());
                        break;

                    case "ELIMINA EN":
                        this.CurrentDatabase.Delete(query, queryElements[2]);
                        break;

                    case "MODIFICA EN":
                        this.CurrentDatabase.Update(query, queryElements[2]);
                        break;

                    default:
                        throw new InstructionNotFoundException();
                }
            Show();

        }
        /// <summary>
        /// Creates a new directory, also called database
        /// </summary>
        /// <param name="name">Name for the new database</param>
        void CreateDatabase(string name) {
            string finalPath = this.RootPath + name;
            if (Directory.Exists(finalPath))
                throw new DatabaseAlreadyExistsException();
            Directory.CreateDirectory(finalPath);
            Console.Write($"Base de datos {name} creada exitosamente...");
            Console.ReadKey();
            LoadDatabases();
        }
        /// <summary>
        /// Deletes selected directory, also called database
        /// </summary>
        /// <param name="name">Name for the database to delete</param>
        void DeleteDatabase(string name) {
            string finalPath = this.RootPath + name;
            Directory.Delete(finalPath, true);
            Console.Write($"Base de datos {name} eliminada correctamente...");
            Console.ReadKey();
            LoadDatabases();
        }
        /// <summary>
        /// Shows all directories from the root path, also called databases
        /// </summary>
        void ShowDatabases() {
            foreach (Database db in this.Directories)
                Console.WriteLine(db.Name);
            Console.ReadKey();
        }
        /// <summary>
        /// Loads all directories from the root path, also called databases
        /// </summary>
        void LoadDatabases() {
            string[] directories = Directory.GetDirectories(this.RootPath);
            this.Directories = new List<Database>();

            foreach (string dbName in directories) {
                DirectoryInfo tempInfo = new DirectoryInfo(dbName);
                this.Directories.Add(new Database(tempInfo.Name, tempInfo.FullName));
            }
        }
        /// <summary>
        /// Puts a database ready to work with it, create tables, add registries, etc...
        /// </summary>
        /// <param name="name">Name of the database to work</param>
        void UseDatabase(string name) {
            string finalPath = this.RootPath + name;
            if (!Directory.Exists(finalPath))
                throw new DatabaseNotExistsException();

            Console.Title = $"Simulador de Base de Datos | By Jonathan Mojica | Root | {name}";
            this.CurrentAction = CurrentState.databaseSelected;
            this.CurrentDatabase = this.Directories.Find(db => db.Location == finalPath);
        }
        void EvaluateList(string compressedQuery) {
            string[] uncompressedQuery = compressedQuery.ToUpper().Trim().Split('\u0020');
            if (uncompressedQuery[1] == "*")
                if (!GetCondition(uncompressedQuery, out string compressedCondition))
                    this.CurrentDatabase.ListAll(uncompressedQuery[3]);
                else
                    this.CurrentDatabase.ListAllWhere(uncompressedQuery[3], compressedCondition);
            else {
                string compressedFields = GetFields(uncompressedQuery, out string tableName);
                if (!GetCondition(uncompressedQuery, out string compressedCondition))
                    this.CurrentDatabase.ListFields(tableName, compressedFields);
                else
                    this.CurrentDatabase.ListFieldsWhere(tableName, compressedFields, compressedCondition);
            }
        }
        bool GetCondition(string[] uncompressedQuery, out string compressedCondition) {
            compressedCondition = null;
            int indexCondition = -1;
            for (int i = uncompressedQuery.Length - 1; i >= 0; i--) {
                if (uncompressedQuery[i].Contains("DONDE")) {
                    indexCondition = i + 1;
                }
            }
            if (indexCondition != -1)
                for (int i = indexCondition; i < uncompressedQuery.Length; i++)
                    compressedCondition += uncompressedQuery[i];
            return indexCondition != -1;
        }
        string GetFields(string[] uncompressedQuery, out string tableName) {
            string compressedFields = null;
            tableName = null;
            uncompressedQuery = uncompressedQuery.Skip(1).ToArray();
            for (int i = 0; i < uncompressedQuery.Length; i++) {
                if (uncompressedQuery[i] == "DE") {
                    tableName = uncompressedQuery[i + 1];
                    break;
                }
                compressedFields += uncompressedQuery[i].Trim();
            }
            return compressedFields;
        }
    }
}
