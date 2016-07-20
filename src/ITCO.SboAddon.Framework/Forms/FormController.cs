using ITCO.SboAddon.Framework.Helpers;
using SAPbouiCOM;
using System;

namespace ITCO.SboAddon.Framework.Forms
{
    /// <summary>
    /// Base Form Controller
    /// </summary>
    public abstract class FormController
    {
        /// <summary>
        /// Form Object
        /// </summary>
        protected IForm Form;

        /// <summary>
        /// Embeded resources name
        /// </summary>
        /// <example>Forms.MyForm.srf</example>
        /// <remarks>In VB Embeded Resources does not have Folder added in Name</remarks>
        public virtual string FormResource { get { return string.Format("Forms.{0}.srf", GetType().Name.Replace("Controller", string.Empty)); } }

        /// <summary>
        /// Eg. NS_MyFormType1
        /// </summary>
        public virtual string FormType { get { return string.Format("ITCO_{0}", GetType().Name.Replace("Controller", string.Empty)); } }

        /// <summary>
        /// Open only once
        /// </summary>
        public virtual bool Unique { get; set; }

        /// <summary>
        /// Create new Form
        /// </summary>        
        public FormController(bool autoStart = false)
        {
            if (autoStart)
                Start();
        }

        /// <summary>
        /// Initalize and show form
        /// </summary>
        public void Start()
        {
            if (Form != null)
                return;

            if (!Unique)
            {
                // Try get existing form
                try
                {
                    var form = SboApp.Application.Forms.Item(FormType);
                    form.Select();
                    //SboApp.Application.MessageBox(string.Format("Form {0} already open", FormType ));
                }
                catch
                {
                    // ignored
                }
            }

            try
            {
                var assembly = GetType().Assembly;
                Form = FormHelper.CreateFormFromResource(FormResource, FormType, Unique ? null : FormType, assembly);

                try
                {
                    FormCreated();
                }
                catch (Exception e)
                {
                    SboApp.Application.MessageBox(string.Format("FormCreated Error: {0}", e.Message));
                }

                try
                {
                    BindFormEvents();
                }
                catch (Exception e)
                {
                    SboApp.Application.MessageBox(string.Format("BindFormEvents Error: {0}", e.Message));
                }

                Form.Visible = true;
            }
            catch (Exception e)
            {
                SboApp.Application.MessageBox(string.Format("Failed to open form {0}: {1}", FormType, e.Message));
            }
        }

        /// <summary>
        /// Close Form
        /// </summary>
        public void Close()
        {
            Form.Close();
            Form = null;
        }

        /// <summary>
        /// Edit Form after it is created
        /// </summary>
        public virtual void FormCreated()
        {
        }

        /// <summary>
        /// Bind Events
        /// </summary>
        public virtual void BindFormEvents()
        {
        }

        #region Default IFormMenuItem
        /// <summary>
        /// Menu Id
        /// </summary>
        public string MenuItemId { get { return string.Format("{0}_M", FormType); } }

        /// <summary>
        /// Menu position, -1 = Last
        /// </summary>
        public int MenuItemPosition { get { return -1; } }
        #endregion
    }
}
