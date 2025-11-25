using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using CinePapers.Controls;
using CinePapers.Forms;
using CinePapers.Models.Common;
using CinePapers.Services;

namespace CinePapers
{
    public partial class Form1 : Form
    {
        private ICinemaService _currentService;
        private ComboBox _cboCinema;
        private TabControl _tabCategory;
        private TextBox _txtSearch;
        private Button _btnSearch;

        private int _currentPage = 1;
        private bool _isLoading = false;
        private bool _isEnded = false;

        public Form1()
        {
            InitializeComponent();
            InitializeCustomUI();

            if (_cboCinema.Items.Count > 0)
                _cboCinema.SelectedIndex = 0;

            Rectangle screen = Screen.PrimaryScreen.WorkingArea;
            int w = (int)(screen.Width * 0.78);
            int h = (int)(screen.Height * 0.78);
            this.Size = new Size(w, h);
            this.StartPosition = FormStartPosition.CenterScreen;
        }

        private void InitializeCustomUI()
        {
            // 상단 헤더
            Panel pnlHeader = new Panel { Dock = DockStyle.Top, Height = 33, Padding = new Padding(5) };
            this.Controls.Add(pnlHeader);

            // 1. 콤보박스
            _cboCinema = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 120, Dock = DockStyle.Right, Font = new Font("맑은 고딕", 9F) };
            foreach (var s in CinemaServiceManager.GetAvailableServices()) _cboCinema.Items.Add(s);
            _cboCinema.DisplayMember = "CinemaName";
            _cboCinema.SelectedIndexChanged += OnCinemaChanged;
            pnlHeader.Controls.Add(_cboCinema);

            // 2. 검색창
            Panel pnlSearch = new Panel { Dock = DockStyle.Right, Width = 250 };
            pnlHeader.Controls.Add(pnlSearch);
            _btnSearch = new Button { Text = "검색", Dock = DockStyle.Right };
            _btnSearch.Click += (s, e) => _ = ReloadEventsAsync();
            pnlSearch.Controls.Add(_btnSearch);
            _txtSearch = new TextBox { Dock = DockStyle.Fill };
            _txtSearch.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) _ = ReloadEventsAsync(); };
            pnlSearch.Controls.Add(_txtSearch);

            // 3. 탭 컨트롤
            _tabCategory = new TabControl { Dock = DockStyle.Fill };
            _tabCategory.SelectedIndexChanged += TabSelectionHandler;
            pnlHeader.Controls.Add(_tabCategory);
            _tabCategory.BringToFront();

            // 4. 리스트 (Designer의 flowLayoutPanel1 사용)
            flowLayoutPanel1.Padding = new Padding(0, 40, 0, 0);
            pnlHeader.BringToFront();
            flowLayoutPanel1.Scroll += OnListScroll;
            flowLayoutPanel1.MouseWheel += OnListScroll;
        }

        private void OnCinemaChanged(object sender, EventArgs e)
        {
            _currentService = _cboCinema.SelectedItem as ICinemaService;
            if (_currentService == null) return;

            _tabCategory.SelectedIndexChanged -= TabSelectionHandler;

            _tabCategory.TabPages.Clear();
            foreach (var cat in _currentService.GetCategories())
            {
                TabPage page = new TabPage(cat.Key) { Tag = cat.Value };
                _tabCategory.TabPages.Add(page);
            }

            if (_tabCategory.TabPages.Count > 0)
                _tabCategory.SelectedIndex = 0;

            _tabCategory.SelectedIndexChanged += TabSelectionHandler;
            _ = ReloadEventsAsync();
        }

        private void TabSelectionHandler(object sender, EventArgs e)
        {
            _ = ReloadEventsAsync();
        }

        private async Task ReloadEventsAsync()
        {
            _currentPage = 1;
            _isEnded = false;
            flowLayoutPanel1.Controls.Clear();
            await LoadEventsAsync();
        }

        private async Task LoadEventsAsync()
        {
            if (_isLoading || _isEnded || _currentService == null) return;

            if (_tabCategory.SelectedTab == null) return;

            _isLoading = true;
            try
            {
                string categoryCode = _tabCategory.SelectedTab.Tag.ToString();
                System.Diagnostics.Debug.WriteLine(categoryCode);
                string keyword = _txtSearch.Text.Trim();

                var events = await _currentService.GetEventsListAsync(categoryCode, _currentPage, keyword);

                if (events == null || events.Count == 0)
                {
                    _isEnded = true;
                    if (_currentPage == 1) MessageBox.Show("조회된 이벤트가 없습니다.");
                }
                else
                {
                    flowLayoutPanel1.SuspendLayout();
                    foreach (var item in events)
                    {
                        var card = new EventCardControl(item);
                        card.CardClicked += OnCardClicked;
                        flowLayoutPanel1.Controls.Add(card);
                    }
                    flowLayoutPanel1.ResumeLayout();
                    _currentPage++;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("로드 오류: " + ex.Message);
            }
            finally { _isLoading = false; }
        }

        private void OnCardClicked(object sender, CinemaEventItem item)
        {
            var popup = new EventDetailPopup(item.EventId, item.Title, _currentService);
            popup.Show();
        }

        private async void OnListScroll(object sender, EventArgs e)
        {
            if (flowLayoutPanel1.VerticalScroll.Value + flowLayoutPanel1.ClientSize.Height >= flowLayoutPanel1.VerticalScroll.Maximum - 50)
                await LoadEventsAsync();
        }
    }
}