using System;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using CinePapers.Models.Common;
using CinePapers.Models.CGV;
using CinePapers.Models.Mega;

namespace CinePapers
{
    public partial class Form1 : Form
    {
        // [핵심] 구체적인 클래스 대신 인터페이스 사용 (다형성)
        private ICinemaService _currentService;

        // UI 컨트롤
        private ComboBox cboCinema;
        private TabControl tabCategory;
        private TextBox txtSearch;
        private Button btnSearch;

        // 상태 변수
        private int _currentPage = 1;
        private bool _isLoading = false;
        private bool _isEnded = false;

        public Form1()
        {
            InitializeComponent();
            InitializeUI(); // UI 동적 생성 및 배치

            // 기본값 설정 (첫 번째 영화관 선택 -> 자동으로 로드됨)
            cboCinema.SelectedIndex = 0;
        }

        // ---------------------------------------------------------
        // [UI 초기화] 상단 탭, 검색창, 콤보박스 배치 (레이아웃 수정됨)
        // ---------------------------------------------------------
        private void InitializeUI()
        {
            // 1. 헤더 패널 (상단 고정)
            Panel pnlHeader = new Panel();
            pnlHeader.Dock = DockStyle.Top;
            pnlHeader.Height = 40;
            pnlHeader.Padding = new Padding(5);
            this.Controls.Add(pnlHeader);

            // -----------------------------------------------------------------------
            // [변경] 2. 영화관 선택 콤보박스 (맨 오른쪽 배치)
            // Dock = Right를 사용하여 가장 먼저 추가하면 화면의 가장 오른쪽에 붙습니다.
            // -----------------------------------------------------------------------
            cboCinema = new ComboBox();
            cboCinema.DropDownStyle = ComboBoxStyle.DropDownList;
            cboCinema.Width = 120;
            cboCinema.Dock = DockStyle.Right; // 왼쪽(Left) -> 오른쪽(Right)으로 변경
            cboCinema.Font = new Font("맑은 고딕", 10F);

            // 서비스 등록
            cboCinema.Items.Add(new CgvCinemaService());
            cboCinema.Items.Add(new LotteCinemaService());
            cboCinema.Items.Add(new MegaCinemaService());
            cboCinema.DisplayMember = "CinemaName";
            cboCinema.SelectedIndexChanged += CboCinema_SelectedIndexChanged;

            // pnlHeader에 가장 먼저 추가해야 맨 오른쪽에 위치함
            pnlHeader.Controls.Add(cboCinema);

            // -----------------------------------------------------------------------
            // [변경] 3. 검색 UI 패널 (콤보박스 바로 왼쪽)
            // Dock = Right로 추가하면 먼저 추가된 cboCinema의 왼쪽에 쌓입니다.
            // -----------------------------------------------------------------------
            Panel pnlSearch = new Panel();
            pnlSearch.Dock = DockStyle.Right; // 오른쪽 정렬
            pnlSearch.Width = 250;
            pnlHeader.Controls.Add(pnlSearch);

            btnSearch = new Button();
            btnSearch.Text = "검색";
            btnSearch.Dock = DockStyle.Right; // 패널 내부에서 오른쪽
            btnSearch.Click += BtnSearch_Click;
            pnlSearch.Controls.Add(btnSearch);

            txtSearch = new TextBox();
            txtSearch.Dock = DockStyle.Fill; // 남은 공간 채움
            txtSearch.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) BtnSearch_Click(s, e); };
            pnlSearch.Controls.Add(txtSearch);

            // 4. 탭 컨트롤 (나머지 왼쪽 공간 채움)
            tabCategory = new TabControl();
            tabCategory.Dock = DockStyle.Fill;
            tabCategory.SelectedIndexChanged += TabCategory_SelectedIndexChanged;
            pnlHeader.Controls.Add(tabCategory);

            // Z-Order 조정: 탭이 남은 공간을 제대로 차지하도록 맨 앞으로 가져오기
            tabCategory.BringToFront();

            // 5. 리스트 패널 (헤더 아래 나머지 공간 채움)
            // flowLayoutPanel1은 디자이너 파일(Form1.Designer.cs)에 이미 있다고 가정
            flowLayoutPanel1.Dock = DockStyle.Fill;
            flowLayoutPanel1.Padding = new Padding(0, 40, 0, 0); // 헤더에 가리지 않게 여백
            pnlHeader.BringToFront(); // 헤더가 리스트보다 위에 오도록

            // 스크롤 설정
            flowLayoutPanel1.AutoScroll = true;
            flowLayoutPanel1.Scroll += FlowLayoutPanel1_Scroll;
            flowLayoutPanel1.MouseWheel += FlowLayoutPanel1_Scroll;
        }

        // ---------------------------------------------------------
        // [이벤트 핸들러]
        // ---------------------------------------------------------

        // [영화관 변경]
        private void CboCinema_SelectedIndexChanged(object sender, EventArgs e)
        {
            // 1. 서비스 교체 (업캐스팅)
            _currentService = (ICinemaService)cboCinema.SelectedItem;

            // 2. 탭 메뉴 재구성 (영화관마다 카테고리 코드가 다름)
            tabCategory.TabPages.Clear();
            var categories = _currentService.GetCategories();
            foreach (var cat in categories)
            {
                TabPage page = new TabPage(cat.Key);
                page.Tag = cat.Value; // 실제 API에 보낼 코드는 Tag에 숨김
                tabCategory.TabPages.Add(page);
            }

            // 3. 데이터 로드 트리거
            if (tabCategory.TabPages.Count > 0)
            {
                tabCategory.SelectedIndex = 0;
                // 탭 변경 이벤트가 발생하지 않을 수 있으므로 수동 호출하여 로드 시작
                TabCategory_SelectedIndexChanged(null, null);
            }
        }

        // [탭 변경]
        private async void TabCategory_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (tabCategory.SelectedTab == null) return;

            // 탭이 바뀌면 검색어와 리스트 초기화
            txtSearch.Text = "";
            _currentPage = 1;
            _isEnded = false;
            flowLayoutPanel1.Controls.Clear();

            await LoadEventsAsync();
        }

        // [검색 버튼 클릭]
        private async void BtnSearch_Click(object sender, EventArgs e)
        {
            // 검색 시 리스트 초기화
            _currentPage = 1;
            _isEnded = false;
            flowLayoutPanel1.Controls.Clear();

            await LoadEventsAsync();
        }

        // [폼 로드]
        private async void Form1_Load_1(object sender, EventArgs e)
        {
            // 생성자에서 콤보박스 선택 시 이미 로드되므로 여기선 생략 가능
            // await LoadEventsAsync(); 
        }

        // ---------------------------------------------------------
        // [데이터 로딩 로직]
        // ---------------------------------------------------------
        private async Task LoadEventsAsync()
        {
            if (_isLoading || _isEnded || _currentService == null) return;

            _isLoading = true;
            try
            {
                // 현재 탭의 카테고리 코드와 검색어 가져오기
                string categoryCode = tabCategory.SelectedTab?.Tag?.ToString() ?? "";
                string keyword = txtSearch.Text.Trim();

                // [핵심] 인터페이스를 통한 다형성 호출 (Lotte, CGV, Mega 모두 동일하게 호출)
                var events = await _currentService.GetEventsListAsync(categoryCode, _currentPage, keyword);

                if (events == null || events.Count == 0)
                {
                    _isEnded = true;
                    // 첫 페이지인데 데이터가 없으면 검색 결과 없음 알림
                    if (_currentPage == 1) MessageBox.Show("조회된 이벤트가 없습니다.");
                }
                else
                {
                    foreach (var item in events)
                    {
                        AddEventCard(item);
                    }
                    _currentPage++; // 다음 페이지 준비
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("로드 중 오류: " + ex.Message);
            }
            finally
            {
                _isLoading = false;
            }
        }

        // [무한 스크롤 감지]
        private async void FlowLayoutPanel1_Scroll(object sender, EventArgs e)
        {
            int totalHeight = flowLayoutPanel1.VerticalScroll.Maximum;
            int visibleHeight = flowLayoutPanel1.ClientSize.Height;
            int currentScroll = flowLayoutPanel1.VerticalScroll.Value;

            // 바닥에 거의 도달했으면(여유 50px) 다음 페이지 로드
            if (currentScroll + visibleHeight >= totalHeight - 50)
                await LoadEventsAsync();
        }

        // ---------------------------------------------------------
        // [UI 생성 로직]
        // ---------------------------------------------------------
        private void AddEventCard(CinemaEventItem item)
        {
            // 카드 패널
            Panel card = new Panel();
            card.Size = new Size(340, 280);
            card.BorderStyle = BorderStyle.FixedSingle;
            card.Margin = new Padding(10);
            card.Cursor = Cursors.Hand;
            card.BackColor = Color.White;

            // 이미지 박스
            PictureBox pb = new PictureBox();
            pb.Location = new Point(10, 10);
            pb.Size = new Size(320, 200);
            pb.SizeMode = PictureBoxSizeMode.Zoom;
            pb.Cursor = Cursors.Hand;
            if (!string.IsNullOrEmpty(item.ImageUrl))
                pb.LoadAsync(item.ImageUrl);
            card.Controls.Add(pb);

            // 제목 라벨
            Label lbl = new Label();
            lbl.Text = item.Title; // 공통 모델의 Title 속성 사용
            lbl.Location = new Point(10, 220);
            lbl.Size = new Size(320, 50);
            lbl.TextAlign = ContentAlignment.TopCenter;
            lbl.Font = new Font("맑은 고딕", 9F, FontStyle.Bold);
            lbl.Cursor = Cursors.Hand;
            card.Controls.Add(lbl);

            // 클릭 이벤트 (상세 팝업 띄우기)
            EventHandler openPopup = (s, e) =>
            {
                // 팝업에 '현재 서비스(_currentService)'와 '이벤트ID'를 넘겨서
                // 팝업 내부에서 해당 영화관에 맞는 상세 정보를 조회하도록 함
                var popup = new ImagePopupForm(item.EventId, item.Title, _currentService);
                popup.Show();
            };

            pb.Click += openPopup;
            lbl.Click += openPopup;
            card.Click += openPopup;

            flowLayoutPanel1.Controls.Add(card);
        }

        // =========================================================
        // [내부 클래스] 상세 팝업 (이미지 + 경품 조회)
        // =========================================================
        public class ImagePopupForm : Form
        {
            private string _eventId;
            private string _giftId;
            private ICinemaService _service;

            private Panel pnlContainer;
            private Label loadingLabel;
            private Panel pnlBottom;
            private Button btnCheckStock;

            public ImagePopupForm(string eventId, string title, ICinemaService service)
            {
                _eventId = eventId;
                _service = service;

                InitializePopupUI(title);
                this.Shown += ImagePopupForm_Shown;
            }

            private void InitializePopupUI(string title)
            {
                this.Size = new Size(600, 800);
                this.Text = title;
                this.StartPosition = FormStartPosition.CenterScreen;
                this.Resize += ImagePopupForm_Resize;

                // 하단 버튼 영역
                pnlBottom = new Panel { Dock = DockStyle.Bottom, Height = 60, Padding = new Padding(10), BackColor = Color.WhiteSmoke };
                this.Controls.Add(pnlBottom);

                btnCheckStock = new Button { Text = "경품 수량 확인", Dock = DockStyle.Fill, Enabled = false };
                btnCheckStock.BackColor = Color.FromArgb(237, 28, 36);
                btnCheckStock.ForeColor = Color.White;
                btnCheckStock.Font = new Font("맑은 고딕", 12F, FontStyle.Bold);
                btnCheckStock.FlatStyle = FlatStyle.Flat;
                btnCheckStock.Click += BtnCheckStock_Click;
                pnlBottom.Controls.Add(btnCheckStock);

                // 스크롤 컨테이너
                pnlContainer = new Panel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = Color.Black, Padding = new Padding(0) };
                this.Controls.Add(pnlContainer);
                pnlContainer.BringToFront();

                // 로딩 라벨
                loadingLabel = new Label { Text = "상세 정보를 불러오는 중...", ForeColor = Color.White, BackColor = Color.Transparent, AutoSize = false, TextAlign = ContentAlignment.MiddleCenter, Dock = DockStyle.Fill };
                pnlContainer.Controls.Add(loadingLabel);
            }

            private async void ImagePopupForm_Shown(object sender, EventArgs e)
            {
                try
                {
                    var detail = await _service.GetEventDetailAsync(_eventId);

                    if (detail != null)
                    {
                        if (detail.ImageUrls != null && detail.ImageUrls.Count > 0)
                        {
                            loadingLabel.Visible = false; // 로딩 숨김

                            // 이미지를 순서대로 추가 (역순으로 추가하여 순서 맞춤)
                            for (int i = detail.ImageUrls.Count - 1; i >= 0; i--)
                            {
                                string url = detail.ImageUrls[i];
                                AddPictureBox(url);
                            }
                        }
                        else
                        {
                            loadingLabel.Text = "이미지가 없습니다.";
                        }

                        // 재고 조회 설정
                        if (detail.HasStockCheck)
                        {
                            _giftId = detail.OriginalGiftId;
                            btnCheckStock.Enabled = true;
                            btnCheckStock.Text = "경품 수량 확인";
                        }
                        else
                        {
                            btnCheckStock.Text = "재고 조회 미지원";
                            btnCheckStock.Enabled = false;
                            btnCheckStock.BackColor = Color.Gray;
                        }
                    }
                    else
                    {
                        MessageBox.Show("상세 정보를 가져올 수 없습니다.");
                        this.Close();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("오류 발생: " + ex.Message);
                    this.Close();
                }
            }

            private void AddPictureBox(string url)
            {
                PictureBox pb = new PictureBox();
                pb.Dock = DockStyle.Top;
                pb.SizeMode = PictureBoxSizeMode.StretchImage;
                pb.Tag = url;

                pnlContainer.Controls.Add(pb);

                pb.LoadAsync(url);
                pb.LoadCompleted += (s, e) =>
                {
                    if (pb.Image != null)
                    {
                        ResizePictureBox(pb);
                    }
                };
            }

            private async void BtnCheckStock_Click(object sender, EventArgs e)
            {
                string originalText = btnCheckStock.Text;
                btnCheckStock.Text = "조회 중...";
                btnCheckStock.Enabled = false;

                try
                {
                    var stockList = await _service.GetGiftStockAsync(_eventId, _giftId);

                    if (stockList.Count > 0)
                    {
                        var msgList = stockList
                            //.Where(s => s.StockCount > 0)
                            .OrderBy(s => s.SortOrder)
                            .Select(s =>
                            {
                                string suffix = s.StockCount >= 50 ? "이상" : "이하";
                                return $"[{s.Region}] {s.CinemaName}: {s.StockCount}개 {suffix}";
                            });

                        var msg = string.Join("\n", msgList);
                        MessageBox.Show(string.IsNullOrEmpty(msg) ? "현재 재고가 남아있는 극장이 없습니다." : msg, "재고 현황");
                    }
                    else
                    {
                        MessageBox.Show("재고 정보를 가져올 수 없습니다.");
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("조회 오류: " + ex.Message);
                }
                finally
                {
                    btnCheckStock.Text = originalText;
                    btnCheckStock.Enabled = true;
                }
            }

            private void ImagePopupForm_Resize(object sender, EventArgs e)
            {
                foreach (Control c in pnlContainer.Controls)
                {
                    if (c is PictureBox pb)
                    {
                        ResizePictureBox(pb);
                    }
                }
            }

            private void ResizePictureBox(PictureBox pb)
            {
                if (pb.Image == null) return;
                int w = pnlContainer.ClientSize.Width;
                float r = (float)pb.Image.Height / pb.Image.Width;
                pb.Height = (int)(w * r);
            }
        }
    }
}