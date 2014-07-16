﻿// <copyright>
// Copyright 2013 by the Spark Development Network
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>
//
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;
using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Security;
using Rock.Web.Cache;
using Rock.Web.UI.Controls;

namespace RockWeb.Blocks.Finance
{
    [DisplayName( "Business List" )]
    [Category( "Finance" )]
    [Description( "Lists all businesses and provides filtering by business name and owner" )]
    [LinkedPage( "Detail Page" )]
    public partial class BusinessList : Rock.Web.UI.RockBlock
    {
        #region Control Methods

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Init" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );

            bool canEdit = IsUserAuthorized( Authorization.EDIT );

            gfBusinessFilter.ApplyFilterClick += gfBusinessFilter_ApplyFilterClick;
            gfBusinessFilter.DisplayFilterValue += gfBusinessFilter_DisplayFilterValue;

            gBusinessList.DataKeyNames = new string[] { "id" };
            gBusinessList.Actions.ShowAdd = canEdit;
            gBusinessList.Actions.AddClick += gBusinessList_AddClick;
            gBusinessList.GridRebind += gBusinessList_GridRebind;
            gBusinessList.IsDeleteEnabled = canEdit;
        }

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Load" /> event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );

            if ( !Page.IsPostBack )
            {
                BindFilter();
                BindGrid();
            }
        }

        #endregion Control Methods

        #region Events

        /// <summary>
        /// Handles the DisplayFilterValue event of the gfBusinessFilter control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="Rock.Web.UI.Controls.GridFilter.DisplayFilterValueArgs"/> instance containing the event data.</param>
        private void gfBusinessFilter_DisplayFilterValue( object sender, Rock.Web.UI.Controls.GridFilter.DisplayFilterValueArgs e )
        {
            switch ( e.Key )
            {
                case "Business Name":
                    break;
            }
        }

        /// <summary>
        /// Handles the ApplyFilterClick event of the gfBusinessFilter control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void gfBusinessFilter_ApplyFilterClick( object sender, EventArgs e )
        {
            gfBusinessFilter.SaveUserPreference( "Business Name", tbBusinessName.Text );
            BindGrid();
        }

        /// <summary>
        /// Handles the GridRebind event of the gBusinessList control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void gBusinessList_GridRebind( object sender, EventArgs e )
        {
            BindGrid();
        }

        /// <summary>
        /// Handles the AddClick event of the gBusinessList control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void gBusinessList_AddClick( object sender, EventArgs e )
        {
            var parms = new Dictionary<string, string>();
            parms.Add( "businessId", "0" );
            NavigateToLinkedPage( "DetailPage", parms );
        }

        /// <summary>
        /// Handles the RowDataBound event of the gBusinessList control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="GridViewRowEventArgs"/> instance containing the event data.</param>
        protected void gBusinessList_RowDataBound( object sender, GridViewRowEventArgs e )
        {
            if ( e.Row.RowType == DataControlRowType.DataRow )
            {
                var rockContext = new RockContext();
                var business = e.Row.DataItem as Person;
                int? ownerRoleId = new GroupTypeRoleService( rockContext ).Queryable()
                    .Where( r =>
                        r.Guid.Equals( new Guid( Rock.SystemGuid.GroupRole.GROUPROLE_KNOWN_RELATIONSHIPS_OWNER ) ) )
                    .Select( r => r.Id )
                    .FirstOrDefault();

                if ( business.PhoneNumbers.Count > 0 )
                {
                    var phoneNumber = business.PhoneNumbers.FirstOrDefault().NumberFormatted;
                    if ( !string.IsNullOrWhiteSpace( phoneNumber ) )
                    {
                        Label lblPhoneNumber = e.Row.FindControl( "lblPhoneNumber" ) as Label;
                        if ( lblPhoneNumber != null )
                        {
                            lblPhoneNumber.Text = string.Format( "{0}</br>", phoneNumber );
                        }
                    }
                }

                if ( !string.IsNullOrWhiteSpace( business.Email ) )
                {
                    Label lblEmail = e.Row.FindControl( "lblEmail" ) as Label;
                    if ( lblEmail != null )
                    {
                        lblEmail.Text = string.Format( "{0}", business.Email );
                    }
                }

                if ( business.GivingGroup.GroupLocations.Any() )
                {
                    Label lblAddress = e.Row.FindControl( "lblAddress" ) as Label;
                    lblAddress.Text = business.GivingGroup.GroupLocations
                        .Select( gl => gl.Location )
                        .FirstOrDefault()
                        .GetFullStreetAddress()
                        .ConvertCrLfToHtmlBr();
                }
            }
        }

        /// <summary>
        /// Handles the RowSelected event of the gBusinessList control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="Rock.Web.UI.Controls.RowEventArgs"/> instance containing the event data.</param>
        protected void gBusinessList_RowSelected( object sender, Rock.Web.UI.Controls.RowEventArgs e )
        {
            var parms = new Dictionary<string, string>();
            var businessId = (int)e.RowKeyValue;
            parms.Add( "businessId", businessId.ToString() );
            NavigateToLinkedPage( "DetailPage", parms );
        }

        /// <summary>
        /// Handles the Edit event of the gBusinessList control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="Rock.Web.UI.Controls.RowEventArgs"/> instance containing the event data.</param>
        protected void gBusinessList_Edit( object sender, Rock.Web.UI.Controls.RowEventArgs e )
        {
            var parms = new Dictionary<string, string>();
            var businessId = (int)e.RowKeyValue;
            parms.Add( "businessId", businessId.ToString() );
            NavigateToLinkedPage( "DetailPage", parms );
        }

        /// <summary>
        /// Handles the Delete event of the gBusinessList control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="Rock.Web.UI.Controls.RowEventArgs"/> instance containing the event data.</param>
        protected void gBusinessList_Delete( object sender, Rock.Web.UI.Controls.RowEventArgs e )
        {
            var rockContext = new RockContext();
            PersonService service = new PersonService( rockContext );
            Person business = service.Get( e.RowKeyId );
            if ( business != null )
            {
                business.RecordStatusValueId = DefinedValueCache.Read( new Guid( Rock.SystemGuid.DefinedValue.PERSON_RECORD_STATUS_INACTIVE ) ).Id;
                rockContext.SaveChanges();
            }

            BindGrid();
        }

        #endregion Events

        #region Internal Methods

        /// <summary>
        /// Binds the filter.
        /// </summary>
        private void BindFilter()
        {
            var rockContext = new RockContext();

            // Business Name Filter
            tbBusinessName.Text = gfBusinessFilter.GetUserPreference( "Business Name" );
        }

        /// <summary>
        /// Binds the grid.
        /// </summary>
        private void BindGrid()
        {
            var rockContext = new RockContext();
            var recordTypeValueId = DefinedValueCache.Read( Rock.SystemGuid.DefinedValue.PERSON_RECORD_TYPE_BUSINESS.AsGuid() ).Id;
            var activeRecordStatusValueId = DefinedValueCache.Read( Rock.SystemGuid.DefinedValue.PERSON_RECORD_STATUS_ACTIVE.AsGuid() ).Id;
            int? businessRoleId = new GroupTypeRoleService( rockContext ).Queryable()
                .Where( r =>
                    r.Guid.Equals( new Guid( Rock.SystemGuid.GroupRole.GROUPROLE_KNOWN_RELATIONSHIPS_BUSINESS ) ) )
                .Select( r => r.Id )
                .FirstOrDefault();
            var queryable = new PersonService( rockContext ).Queryable()
                .Where( q => q.RecordTypeValueId == recordTypeValueId && q.RecordStatusValueId == activeRecordStatusValueId );

            // Business Name Filter
            var businessName = gfBusinessFilter.GetUserPreference( "Business Name" );
            if ( !string.IsNullOrWhiteSpace( businessName ) )
            {
                queryable = queryable.Where( a => a.FirstName.Contains( businessName ) );
            }

            SortProperty sortProperty = gBusinessList.SortProperty;
            if ( sortProperty != null )
            {
                gBusinessList.DataSource = queryable.Sort( sortProperty ).ToList();
            }
            else
            {
                gBusinessList.DataSource = queryable.OrderBy( q => q.FirstName ).ToList();
            }

            gBusinessList.DataBind();
        }

        /// <summary>
        /// Shows the detail form.
        /// </summary>
        /// <param name="id">The id.</param>
        protected void ShowDetailForm( int id )
        {
            NavigateToLinkedPage( "DetailPage", "businessId", id );
        }

        #endregion Internal Methods
    }
}