using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using CinePapers.Controls;
using CinePapers.Forms;
using CinePapers.Models.Common;
using CinePapers.Services;
using CinePapers.ViewModels;
using CinePapers.Models.CGV_WebView; // 네임스페이스 추가

namespace CinePapers
{
    public partial class Form1 : Form
    {
        private MainViewModel _viewModel = new MainViewModel();

        // [수정 1] 클래스 멤버 변수로 승격 (어디서든 접근 가능하도록)
        private CgvWebViewService _cgvService;
        private bool _isReady = false; // 초기화 완료 여부 체크용

        // UI 컨트롤
        private ComboBox _cboCinema;
        private TabControl _tabCategory;
        private TextBox _txtSearch;
        private Button _btnSearch;

        public Form1()
        {
            InitializeComponent();

            // 1. 화면 크기 설정
            Rectangle screen = Screen.PrimaryScreen.WorkingArea;
            int w = (int)(screen.Width * 0.78);
            int h = (int)(screen.Height * 0.78);
            this.Size = new Size(w, h);
            this.StartPosition = FormStartPosition.CenterScreen;

            // [수정 2] 서비스 인스턴스 생성 (딱 한 번만)
            _cgvService = new CgvWebViewService();

            // 2. UI 초기화
            InitializeCustomUI();

            // [수정 3] Load 이벤트에서 초기화와 첫 데이터 로드를 진행 (순서 보장)
            this.Load += OnFormLoad;
        }

        private async void OnFormLoad(object sender, EventArgs e)
        {
            // 1. WebView 초기화 대기
            await _cgvService.InitializeAsync(this);

            // 2. 준비 완료 플래그 켜기
            _isReady = true;

            // 3. 콤보박스 첫 번째 항목 선택 -> 이때 OnCinemaChanged가 실행됨
            if (_cboCinema.Items.Count > 0)
                _cboCinema.SelectedIndex = 0;
        }

        private void InitializeCustomUI()
        {
            // 상단 헤더 패널
            Panel pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 31,
                Padding = new Padding(5, 5, 5, 0)
            };
            this.Controls.Add(pnlHeader);

            // 영화관 선택 콤보박스
            _cboCinema = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width = 120,
                Dock = DockStyle.Right,
                Font = new Font("맑은 고딕", 10F)
            };

            // [수정 4] 서비스 매니저 대신(혹은 같이), 내가 만든 _cgvService를 리스트에 추가
            // (CinemaServiceManager.GetAvailableServices()에서 CGV를 뺀 리스트를 가져오거나, 여기서 직접 추가)

            // 예: 롯데, 메가박스 등 다른 서비스 추가
            foreach (var s in CinemaServiceManager.GetAvailableServices())
            {
                // CGV가 중복되지 않도록 CinemaName 체크 (선택 사항)
                if (s.CinemaName != "CGV")
                    _cboCinema.Items.Add(s);
            }

            // ★ 중요: 초기화할 _cgvService 인스턴스를 직접 추가해야 함
            _cboCinema.Items.Add(_cgvService);

            _cboCinema.DisplayMember = "CinemaName";
            _cboCinema.SelectedIndexChanged += OnCinemaChanged;
            pnlHeader.Controls.Add(_cboCinema);

            // ... (나머지 검색 UI, 탭 컨트롤, 리스트 패널 코드는 기존과 동일) ...

            // 검색 UI
            Panel pnlSearch = new Panel { Dock = DockStyle.Right, Width = 260, Padding = new Padding(10, 0, 0, 0) };
            pnlHeader.Controls.Add(pnlSearch);

            _btnSearch = new Button { Text = "검색", Dock = DockStyle.Right, Width = 60, Cursor = Cursors.Hand };
            _btnSearch.Click += (s, e) => _ = RequestDataAsync(isReload: true);
            pnlSearch.Controls.Add(_btnSearch);

            _txtSearch = new TextBox { Dock = DockStyle.Fill, Font = new Font("맑은 고딕", 10F) };
            _txtSearch.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) _ = RequestDataAsync(isReload: true); };
            pnlSearch.Controls.Add(_txtSearch);

            // 탭 컨트롤
            _tabCategory = new TabControl { Dock = DockStyle.Fill, Height = 15 };
            _tabCategory.SelectedIndexChanged += TabSelectionHandler;
            pnlHeader.Controls.Add(_tabCategory);
            _tabCategory.BringToFront();

            // 리스트 패널
            flowLayoutPanel1.Padding = new Padding(0, 35, 0, 0);
            pnlHeader.BringToFront();

            // 스크롤 이벤트 연결
            flowLayoutPanel1.Scroll += OnListScroll;
            flowLayoutPanel1.MouseWheel += OnListScroll;
        }

        private void OnCinemaChanged(object sender, EventArgs e)
        {
            // [수정 5] 초기화가 안 끝났으면 로직 중단 (오류 방지)
            if (!_isReady) return;

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
                _tabCategory.SelectedIndex = 0;

            _tabCategory.SelectedIndexChanged += TabSelectionHandler;

            _ = RequestDataAsync(isReload: true);
        }

        // ... (나머지 이벤트 핸들러들은 그대로 유지) ...
        private void TabSelectionHandler(object sender, EventArgs e) { _ = RequestDataAsync(isReload: true); }

        private async Task RequestDataAsync(bool isReload)
        {
            if (_tabCategory.SelectedTab == null || _viewModel.IsLoading) return;

            string categoryCode = _tabCategory.SelectedTab.Tag.ToString();
            string keyword = _txtSearch.Text.Trim();

            if (isReload) flowLayoutPanel1.Controls.Clear();

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
            }
            else if (isReload)
            {
                MessageBox.Show("조회된 이벤트가 없습니다.");
            }
        }

        private void OnCardClicked(object sender, CinemaEventItem item)
        {
            var popup = new EventDetailPopup(item.EventId, item.Title, _viewModel.CurrentService);
            popup.Show();
        }

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
    }
}