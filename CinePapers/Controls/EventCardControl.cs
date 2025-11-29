using CinePapers.Models.Common;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace CinePapers.Controls
{
    public class EventCardControl : UserControl
    {
        public CinemaEventItem EventData { get; private set; }
        public event EventHandler<CinemaEventItem> CardClicked;

        private PictureBox _pbImage;
        private Label _lblTitle;

        public EventCardControl(CinemaEventItem item)
        {
            EventData = item;
            InitializeUI();
            SetData();
        }

        private void InitializeUI()
        {
            this.Size = new Size(340, 280);
            this.BorderStyle = BorderStyle.FixedSingle;
            this.Margin = new Padding(10);
            this.Cursor = Cursors.Hand;
            this.BackColor = Color.White;

            _pbImage = new PictureBox
            {
                Location = new Point(10, 10),
                Size = new Size(320, 200),
                SizeMode = PictureBoxSizeMode.Zoom,
                Cursor = Cursors.Hand,
            };

            _lblTitle = new Label
            {
                Location = new Point(10, 220),
                Size = new Size(320, 50),
                TextAlign = ContentAlignment.TopCenter,
                Font = new Font("맑은 고딕", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };

            this.Controls.Add(_pbImage);
            this.Controls.Add(_lblTitle);

            this.Click += (s, e) => OnCardClicked();
            _pbImage.Click += (s, e) => OnCardClicked();
            _lblTitle.Click += (s, e) => OnCardClicked();
        }

        private void SetData()
        {
            _lblTitle.Text = EventData.Title;
            if (!string.IsNullOrEmpty(EventData.ImageUrl))
                _pbImage.LoadAsync(EventData.ImageUrl);
        }

        private void OnCardClicked()
        {
            CardClicked?.Invoke(this, EventData);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_pbImage != null && _pbImage.Image != null)
                {
                    _pbImage.Image.Dispose();
                    _pbImage.Image = null;
                }
            }
            base.Dispose(disposing);
        }
    }
}