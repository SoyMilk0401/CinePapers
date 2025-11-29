using CinePapers.Models.Common;
using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace CinePapers.Forms
{
    public class EventDetailPopup : Form
    {
        private readonly string _eventId;
        private string _giftId;
        private readonly ICinemaService _service;

        private Panel _pnlContainer;
        private Panel _pnlBottom;
        private Label _loadingLabel;
        private Button _btnCheckStock;

        public EventDetailPopup(string eventId, string title, ICinemaService service, Point? location = null)
        {
            _eventId = eventId;
            _service = service;
            InitializeUI(title, location);
            this.Shown += OnFormShown;
        }

        private void InitializeUI(string title, Point? location)
        {
            this.Size = new Size(800, 1000);
            this.Text = title;

            if (location.HasValue)
            {
                this.StartPosition = FormStartPosition.Manual;
                this.Location = location.Value;
            }
            else
            {
                this.StartPosition = FormStartPosition.CenterScreen;
            }

            this.Resize += (s, e) => ResizeImages();

            _pnlBottom = new Panel { Dock = DockStyle.Bottom, Height = 60, Padding = new Padding(10), BackColor = Color.WhiteSmoke };
            this.Controls.Add(_pnlBottom);

            _btnCheckStock = new Button
            {
                Text = "경품 수량 확인",
                Dock = DockStyle.Fill,
                Enabled = false,
                BackColor = Color.Gray,
                ForeColor = Color.White,
                Font = new Font("맑은 고딕", 12F, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat
            };
            _btnCheckStock.Click += BtnCheckStock_Click;
            _pnlBottom.Controls.Add(_btnCheckStock);

            _pnlContainer = new Panel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = Color.Black };
            this.Controls.Add(_pnlContainer);
            _pnlContainer.BringToFront();

            _loadingLabel = new Label { Text = "상세 정보를 불러오는 중...", ForeColor = Color.White, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter };
            _pnlContainer.Controls.Add(_loadingLabel);
        }

        private async void OnFormShown(object sender, EventArgs e)
        {
            try
            {
                var detail = await _service.GetEventDetailAsync(_eventId);
                if (detail == null) return;

                _loadingLabel.Visible = false;

                if (detail.ImageUrls == null || detail.ImageUrls.Count == 0)
                {
                    _pnlContainer.Visible = false;

                    this.ClientSize = new Size(this.ClientSize.Width, _pnlBottom.Height);

                    this.CenterToScreen();
                }
                else
                {
                    for (int i = detail.ImageUrls.Count - 1; i >= 0; i--)
                        AddPictureBox(detail.ImageUrls[i]);
                }

                if (detail.HasStockCheck)
                {
                    _giftId = detail.OriginalGiftId;
                    _btnCheckStock.Enabled = true;
                    _btnCheckStock.BackColor = Color.FromArgb(237, 28, 36);
                }
                else
                {
                    _btnCheckStock.Text = "재고 조회 미지원";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("오류: " + ex.Message);
                Close();
            }
        }

        private void AddPictureBox(string url)
        {
            var pb = new PictureBox { Dock = DockStyle.Top, SizeMode = PictureBoxSizeMode.StretchImage };
            _pnlContainer.Controls.Add(pb);
            pb.LoadAsync(url);
            pb.LoadCompleted += (s, e) => ResizeSinglePB(pb);
        }

        private void ResizeImages()
        {
            foreach (Control c in _pnlContainer.Controls) if (c is PictureBox pb) ResizeSinglePB(pb);
        }

        private void ResizeSinglePB(PictureBox pb)
        {
            if (pb.Image == null) return;
            int w = _pnlContainer.ClientSize.Width;
            if (pb.Image.Width > 0)
            {
                pb.Height = (int)(w * ((float)pb.Image.Height / pb.Image.Width));
            }
        }

        private async void BtnCheckStock_Click(object sender, EventArgs e)
        {
            _btnCheckStock.Enabled = false;
            _btnCheckStock.Text = "조회 중...";
            try
            {
                var stocks = await _service.GetGiftStockAsync(_eventId, _giftId);
                if (stocks != null && stocks.Count > 0)
                {
                    var msg = string.Join("\n", stocks
                        .OrderBy(s => s.SortOrder)
                        .Select(s =>
                        {
                            string stockText = _service.GetStockStatusText(s.StockCount);
                            return $"[{s.Region}] {s.CinemaName}: {stockText}";
                        }));

                    MessageBox.Show(msg, "재고 현황");
                }
                else
                {
                    MessageBox.Show("재고 정보가 없습니다.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("조회 실패: " + ex.Message);
            }
            finally
            {
                _btnCheckStock.Enabled = true;
                _btnCheckStock.Text = "경품 수량 확인";
            }
        }
    }
}