using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using CinePapers.Controls;
using CinePapers.Forms;
using CinePapers.Models.Common;
using CinePapers.Services;
using CinePapers.ViewModels;

namespace CinePapers
{
    public partial class Form1 : Form
    {
        private MainViewModel _viewModel = new MainViewModel();

        private ComboBox _cboCinema;
        private TabControl _tabCategory;
        private TextBox _txtSearch;
        private Button _btnSearch;

        public Form1()
        {
            InitializeComponent();

            Rectangle screen = Screen.PrimaryScreen.WorkingArea;
            int w = (int)(screen.Width * 0.78);
            int h = (int)(screen.Height * 0.78);
            this.Size = new Size(w, h);
            this.StartPosition = FormStartPosition.CenterScreen;

            InitializeCustomUI();

            if (_cboCinema.Items.Count > 0)
                _cboCinema.SelectedIndex = 0;
        }

        private void InitializeCustomUI()
        {
            Panel pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 31,
                Padding = new Padding(5, 5, 15, 0)
            };
            this.Controls.Add(pnlHeader);

            _cboCinema = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width = 120,
                Dock = DockStyle.Right,
                Font = new Font("맑은 고딕", 10F)
            };

            foreach (var s in CinemaServiceManager.GetAvailableServices())
            {
                _cboCinema.Items.Add(s);
            }
            _cboCinema.DisplayMember = "CinemaName";
            _cboCinema.SelectedIndexChanged += OnCinemaChanged;
            pnlHeader.Controls.Add(_cboCinema);

            Panel pnlSearch = new Panel { Dock = DockStyle.Right, Width = 260, Padding = new Padding(10, 0, 0, 0) };
            pnlHeader.Controls.Add(pnlSearch);

            _btnSearch = new Button { Text = "검색", Dock = DockStyle.Right, Width = 60, Cursor = Cursors.Hand };
            _btnSearch.Click += (s, e) => _ = RequestDataAsync(isReload: true);
            pnlSearch.Controls.Add(_btnSearch);

            _txtSearch = new TextBox { Dock = DockStyle.Fill, Font = new Font("맑은 고딕", 10F) };
            _txtSearch.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) _ = RequestDataAsync(isReload: true); };
            pnlSearch.Controls.Add(_txtSearch);

            _tabCategory = new TabControl { Dock = DockStyle.Fill, Height = 15 };
            _tabCategory.SelectedIndexChanged += TabSelectionHandler;
            pnlHeader.Controls.Add(_tabCategory);
            _tabCategory.BringToFront();

            flowLayoutPanel1.Padding = new Padding(0, 35, 0, 0);
            pnlHeader.BringToFront();

            flowLayoutPanel1.Scroll += OnListScroll;
            flowLayoutPanel1.MouseWheel += OnListScroll;
        }

        // 콤보박스 영화관 변경시
        private void OnCinemaChanged(object sender, EventArgs e)
        {
            var selectedService = _cboCinema.SelectedItem as ICinemaService;
            if (selectedService == null) return;

            _viewModel.CurrentService = selectedService;

            _tabCategory.SelectedIndexChanged -= TabSelectionHandler;

            _tabCategory.TabPages.Clear();
            foreach (var cat in _viewModel.CurrentService.GetCategories())
            {
                TabPage page = new TabPage(cat.Key) { Tag = cat.Value };
                _tabCategory.TabPages.Add(page);
            }

            if (_tabCategory.TabPages.Count > 0)
                _tabCategory.SelectedIndex = 1;

            _tabCategory.SelectedIndexChanged += TabSelectionHandler;

            _ = RequestDataAsync(isReload: true);
        }

        // 탭 카테고리 변경시
        private void TabSelectionHandler(object sender, EventArgs e)
        {
            _ = RequestDataAsync(isReload: true);
        }

        // 데이터 조회 요청
        private async Task RequestDataAsync(bool isReload)
        {
            if (_tabCategory.SelectedTab == null || _viewModel.IsLoading) return;

            string categoryCode = _tabCategory.SelectedTab.Tag.ToString();
            string keyword = _txtSearch.Text.Trim();

            if (isReload)
            {
                flowLayoutPanel1.Controls.Clear();
                // 탭 이동이나 검색 시 스크롤 위치 초기화
                flowLayoutPanel1.AutoScrollPosition = new Point(0, 0);
            }

            var events = await _viewModel.LoadEventsAsync(categoryCode, keyword, isReload);

            if (events != null && events.Count > 0)
            {
                flowLayoutPanel1.SuspendLayout();
                foreach (var item in events)
                {
                    var card = new EventCardControl(item);
                    card.CardClicked += OnCardClicked;
                    flowLayoutPanel1.Controls.Add(card);
                }
                flowLayoutPanel1.ResumeLayout();
                flowLayoutPanel1.PerformLayout();

                await CheckAndFillScreenAsync();
            }
            else if (isReload)
            {
                // [수정됨] 팝업 대신 패널 내부에 "데이터 없음" 라벨 추가
                ShowNoDataMessage();
            }
        }

        // [신규] 데이터 없음 메시지 출력 헬퍼 메서드
        private void ShowNoDataMessage()
        {
            Label lblNoData = new Label
            {
                Text = "조회된 이벤트가 없습니다.",
                ForeColor = Color.Gray,
                Font = new Font("맑은 고딕", 12F, FontStyle.Bold),
                AutoSize = false, // 너비를 수동으로 지정하기 위해 false 설정
                Width = flowLayoutPanel1.ClientSize.Width - 10, // 패널 가로폭에 맞춤 (스크롤바 여유 고려)
                Height = 100, // 적당한 높이
                TextAlign = ContentAlignment.MiddleCenter, // 텍스트 가운데 정렬
                Margin = new Padding(0, 50, 0, 0) // 위쪽 여백을 줘서 너무 붙지 않게 함
            };

            flowLayoutPanel1.Controls.Add(lblNoData);
        }

        // 카드 클릭시 디테일 페이지 팝업
        private void OnCardClicked(object sender, CinemaEventItem item)
        {
            var popup = new EventDetailPopup(item.EventId, item.Title, _viewModel.CurrentService);
            popup.Show();
        }

        // 무한스크롤
        private async void OnListScroll(object sender, EventArgs e)
        {
            if (IsScrollAtBottom())
            {
                await RequestDataAsync(isReload: false);
            }
        }

        private bool IsScrollAtBottom()
        {
            int totalHeight = flowLayoutPanel1.VerticalScroll.Maximum;
            int visibleHeight = flowLayoutPanel1.ClientSize.Height;
            int currentScroll = flowLayoutPanel1.VerticalScroll.Value;

            return currentScroll + visibleHeight >= totalHeight - 50;
        }

        private async Task CheckAndFillScreenAsync()
        {
            bool hasScrollbar = flowLayoutPanel1.DisplayRectangle.Height > flowLayoutPanel1.ClientSize.Height;

            if (!hasScrollbar)
            {
                await RequestDataAsync(false);
            }
        }
    }
}