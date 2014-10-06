using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Data.SqlClient;
using System.Configuration;
using System.Windows.Forms;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;  // bibliothèque pour les expressions régulières
using MaisonDesLigues;



namespace BaseDeDonnees
{
    class Bdd
    {
        //
        // propriétés membres
        //
        private SqlConnection cn;
        private SqlCommand UneSqlCommand;
        private SqlDataAdapter UnSqlDataAdapter;
        private DataTable UneDataTable;
        private SqlTransaction UneSqlTransaction;
        //
        // méthodes
        //
        /// <summary>
        /// constructeur de la connexion
        /// </summary>
        /// <param name="UnLogin">login utilisateur</param>
        /// <param name="UnPwd">mot de passe utilisateur</param>
        public Bdd(String UnLogin, String UnPwd)
        {
            try
            {
                /// <remarks>on commence par récupérer dans CnString les informations contenues dans le fichier app.config
                /// pour la connectionString de nom StrConnMdl
                /// </remarks>
                ConnectionStringSettings CnString = ConfigurationManager.ConnectionStrings["StrConnMdl"];
                ///<remarks>
                /// on va remplacer dans la chaine de connexion les paramètres par le login et le pwd saisis
                ///dans les zones de texte. Pour ça on va utiliser la méthode Format de la classe String.                /// 
                /// </remarks>
                cn = new SqlConnection(string.Format(CnString.ConnectionString, UnLogin, UnPwd));
                cn.Open();
            }
            catch (SqlException Oex)
            {
                throw new Exception("Erreur à la connexion" + Oex.Message);
            }
        }
        /// <summary>
        /// Méthode permettant de fermer la connexion
        /// </summary>
        public void FermerConnexion()
        {
            this.cn.Close();
        }
        /// <summary>
        /// méthode permettant de renvoyer un message d'erreur provenant de la bd
        /// après l'avoir formatté. On ne renvoie que le message, sans code erreur
        /// </summary>
        /// <param name="unMessage">message à formater</param>
        /// <returns>message formaté à afficher dans l'application</returns>
        private String GetMessageSql(String unMessage)
        {
            String[] message = Regex.Split(unMessage, "SQLSERVER-");
            return (Regex.Split(message[1], ":"))[1];
        }
        /// <summary>
        /// permet de récupérer le contenu d'une table ou d'une vue. 
        /// </summary>
        /// <param name="UneTableOuVue"> nom de la table ou la vue dont on veut récupérer le contenu</param>
        /// <returns>un objet de type datatable contenant les données récupérées</returns>
        public DataTable ObtenirDonnees(String UneTableOuVue)
        {
            string Sql = "select * from " + UneTableOuVue;
            this.UneSqlCommand = new SqlCommand(Sql, cn);
            UnSqlDataAdapter = new SqlDataAdapter();
            UnSqlDataAdapter.SelectCommand = this.UneSqlCommand;
            UneDataTable = new DataTable();
            UnSqlDataAdapter.Fill(UneDataTable);
            return UneDataTable;
        }
        /// <summary>
        /// méthode privée permettant de valoriser les paramètres d'un objet commmand communs aux licenciés, bénévoles et intervenants
        /// </summary>
        /// <param name="Cmd">nom de l'objet command concerné par les paramètres</param>
        /// <param name="pNom">nom du participant</param>
        /// <param name="pPrenom">prénom du participant</param>
        /// <param name="pAdresse1">adresse1 du participant</param>
        /// <param name="pAdresse2">adresse2 du participant</param>
        /// <param name="pCp">cp du participant</param>
        /// <param name="pVille">ville du participant</param>
        /// <param name="pTel">téléphone du participant</param>
        /// <param name="pMail">mail du participant</param>
        private void ParamCommunsNouveauxParticipants(SqlCommand Cmd, String pNom, String pPrenom, String pAdresse1, String pAdresse2, String pCp, String pVille, String pTel, String pMail)
        {
            Cmd.Parameters.Add("@pNom", SqlDbType.VarChar).Value = pNom;
            Cmd.Parameters.Add("@pPrenom", SqlDbType.VarChar).Value = pPrenom;
            Cmd.Parameters.Add("@pAdr1", SqlDbType.VarChar).Value = pAdresse1;
            Cmd.Parameters.Add("@pAdr2", SqlDbType.VarChar).Value = pAdresse2;
            Cmd.Parameters.Add("@pCp", SqlDbType.VarChar).Value = pCp;
            Cmd.Parameters.Add("@pVille", SqlDbType.VarChar).Value = pVille;
            Cmd.Parameters.Add("@pTel", SqlDbType.VarChar).Value = pTel;
            Cmd.Parameters.Add("@pMail", SqlDbType.VarChar).Value = pMail;
        }
    
        /// <summary>
        /// Procédure publique qui va appeler la procédure stockée permettant d'inscrire un nouvel intervenant sans nuité
        /// </summary>
        /// <param name="Cmd">nom de l'objet command concerné par les paramètres</param>
        /// <param name="pNom">nom du participant</param>
        /// <param name="pPrenom">prénom du participant</param>
        /// <param name="pAdresse1">adresse1 du participant</param>
        /// <param name="pAdresse2">adresse2 du participant</param>
        /// <param name="pCp">cp du participant</param>
        /// <param name="pVille">ville du participant</param>
        /// <param name="pTel">téléphone du participant</param>
        /// <param name="pMail">mail du participant</param>
        /// <param name="pIdAtelier"> Id de l'atelier où interviendra l'intervenant</param>
        /// <param name="pIdStatut">statut de l'intervenant pour l'atelier : animateur ou intervenant ('ANI' ou 'INT')</param>
        public void InscrireIntervenant(String pNom, String pPrenom, String pAdresse1, String pAdresse2, String pCp, String pVille, String pTel, String pMail, Int16 pIdAtelier, String pIdStatut)
        {
            /// <remarks>
            /// procédure qui va créer :
            /// 1- un enregistrement dans la table participant avec typeParticipant à 'I'
            ///  en cas d'erreurSQL, appel à la méthode GetMessageSql dont le rôle est d'extraire uniquement le message renvoyé
            /// par une procédure ou un trigger SQLSERVER
            /// </remarks>
            /// 
            String MessageErreur = "";
            try
            {
                UneSqlCommand = new SqlCommand("PSnouvelintervenant", cn);
                UneSqlCommand.CommandType = CommandType.StoredProcedure;
                // début de la transaction SqlServer il vaut mieux gérer les transactions dans l'applicatif que dans la bd dans les procédures stockées.
               UneSqlTransaction = this.cn.BeginTransaction();
               this.UneSqlCommand.Transaction = UneSqlTransaction;
                // on appelle la procédure ParamCommunsNouveauxParticipants pour charger les paramètres communs aux Participants
                this.ParamCommunsNouveauxParticipants(UneSqlCommand, pNom, pPrenom, pAdresse1, pAdresse2, pCp, pVille, pTel, pMail);
                // on complète les paramètres spécifiques à l'intervenant
                this.UneSqlCommand.Parameters.Add("@ptype", SqlDbType.VarChar).Value = "I";   // "I" pour le type du participant (Intervenant)
                this.UneSqlCommand.Parameters.Add("@pidatelierintervenant", SqlDbType.Int).Value = pIdAtelier;
                this.UneSqlCommand.Parameters.Add("@pIdStatut", SqlDbType.VarChar).Value = pIdStatut;
                //execution
                UneSqlCommand.ExecuteNonQuery();
                // fin de la transaction. Si on arrive à ce point, c'est qu'aucune exception n'a été levée
                UneSqlTransaction.Commit();
            }
            catch (SqlException Oex)
            {
                MessageErreur = "Erreur SqlServer \n" + this.GetMessageSql(Oex.Message);
             }
            catch (Exception ex)
            {

                MessageErreur = ex.Message + "Autre Erreur, les informations n'ont pas été correctement saisies";
            }
            finally
            {
               if (MessageErreur.Length > 0)
                {
                    // annulation de la transaction
                   UneSqlTransaction.Rollback();
                    // Déclenchement de l'exception
                   throw new Exception(MessageErreur);
                }
            }
        }
        /// <summary>
        /// Procédure publique qui va appeler la procédure stockée permettant d'inscrire un nouvel intervenant qui aura des nuités
        /// </summary>
        /// <param name="Cmd">nom de l'objet command concerné par les paramètres</param>
        /// <param name="pNom">nom du participant</param>
        /// <param name="pPrenom">prénom du participant</param>
        /// <param name="pAdresse1">adresse1 du participant</param>
        /// <param name="pAdresse2">adresse2 du participant</param>
        /// <param name="pCp">cp du participant</param>
        /// <param name="pVille">ville du participant</param>
        /// <param name="pTel">téléphone du participant</param>
        /// <param name="pMail">mail du participant</param>
        /// <param name="pIdAtelier"> Id de l'atelier où interviendra l'intervenant</param>
        /// <param name="pIdStatut">statut de l'intervenant pour l'atelier : animateur ou intervenant</param>
        /// <param name="pLesCategories">tableau contenant la catégorie de chambre pour chaque nuité à réserver</param>
        /// <param name="pLesHotels">tableau contenant l'hôtel pour chaque nuité à réserver</param>
        /// <param name="pLesNuits">tableau contenant l'id de la date d'arrivée pour chaque nuité à réserver</param>
        public void InscrireIntervenant(String pNom, String pPrenom, String pAdresse1, String pAdresse2, String pCp, String pVille, String pTel, String pMail, Int16 pIdAtelier, String pIdStatut, Collection<string> pLesCategories, Collection<string> pLesHotels, Collection<Int16>pLesNuits)
        {
            // surcharge de la procédure InscrireIntervenant
        //    /// <remarks>
        //    /// procédure qui va  :
        //    /// 1- faire appel à la procédure stockée PSnouvelintervenant qui insère un enregistrement dans la table participant
        //    /// 2- va insérer un à 2 enregistrements dans la table CONTENUHEBERGEMENT à l'aide d"une procédure stockée
        //    /// </remarks>
        //    /// 
        // String MessageErreur="";
        //  try
        //   {                
      
        //         UneSqlCommand = new SqlCommand("nouvelintervenant", cn);
        //           UneSqlCommand.CommandType = CommandType.StoredProcedure;
        //       // début de la transactionSqlServer : il vaut mieyx gérer les transactions dans l'applicatif que dans la bd.
        //        UneSqlTransaction = this.cn.BeginTransaction();
        //         this.ParamCommunsNouveauxParticipants(UneSqlCommand, pNom, pPrenom, pAdresse1, pAdresse2, pCp, pVille, pTel, pMail);


              //On va créer ici les paramètres spécifiques à l'inscription d'un intervenant qui réserve des nuits d'hôtel.
             // Paramètre qui stocke les catégories sélectionnées
       //.....................
               
              // Paramètre qui stocke les hotels sélectionnées
      //...........................
                
              // Paramètres qui stocke les nuits sélectionnées
       
      //.......................................         
        //    }
        //    catch (SqlException Oex)
        //    {
        //        //MessageErreur="Erreur Oracle \n" + this.GetMessageOracle(Oex.Message);
        //        MessageBox.Show(Oex.Message);
        //    }
        //    catch (Exception ex)
        //    {
                
        //        MessageErreur= "Autre Erreur, les informations n'ont pas été correctement saisies";
        //    }
        //    finally
        //    {
        //        if (MessageErreur.Length > 0)
        //        {
        //            // annulation de la transaction
        //            UneSqlTransaction.Rollback();
        //            // Déclenchement de l'exception
        //            throw new Exception(MessageErreur);
        //        }             
        //    }
        }
        /// <summary>
        /// fonction permettant de construire un dictionnaire dont l'id est l'id d'une nuité et le contenu une date
        /// sous la la forme : lundi 7 janvier 2014        /// 
        /// </summary>
        /// <returns>un dictionnaire dont l'id est l'id d'une nuité et le contenu une date</returns>
        public Dictionary<Int16, String> ObtenirDatesNuitees()
        {
            Dictionary<Int16, String> LesDatesARetourner = new Dictionary<Int16, String>();
            DataTable LesDatesNuitees = this.ObtenirDonnees("VDATENUITEE02");
            foreach (DataRow UneLigne in LesDatesNuitees.Rows)
            {
                LesDatesARetourner.Add(System.Convert.ToInt16(UneLigne["ID"]), UneLigne["DATEARRIVEENUITEE"].ToString());
            }
            return LesDatesARetourner;

        }
        /// <summary>
        /// procédure qui va se charger d'invoquer la procédure stockée qui ira inscrire un participant de type bénévole
        /// </summary>
        /// <param name="Cmd">nom de l'objet command concerné par les paramètres</param>
        /// <param name="pNom">nom du participant</param>
        /// <param name="pPrenom">prénom du participant</param>
        /// <param name="pAdresse1">adresse1 du participant</param>
        /// <param name="pAdresse2">adresse2 du participant</param>
        /// <param name="pCp">cp du participant</param>
        /// <param name="pVille">ville du participant</param>
        /// <param name="pTel">téléphone du participant</param>
        /// <param name="pMail">mail du participant</param>
        /// <param name="pDateNaissance">mail du bénévole</param>
        /// <param name="pNumeroLicence">numéro de licence du bénévole ou null</param>
        /// <param name="pDateBenevolat">collection des id des dates où le bénévole sera présent</param>
        public void InscrireBenevole(String pNom, String pPrenom, String pAdresse1, String pAdresse2, String pCp, String pVille, String pTel, String pMail, DateTime pDateNaissance, Int64? pNumeroLicence, Collection<Int16> pDateBenevolat)
        {


        }

    }
}
