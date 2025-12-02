using System;
using System.Windows.Forms;

namespace RailwayApp
{
    public partial class EditForm : Form
    {
        private readonly Station _station;
        private readonly Tariff _editedTariff;

        public EditForm(Station station)
        {
            InitializeComponent();
            _station = station;
            _editedTariff = null;
            Text = "Добавить новый тариф";
        }

        public EditForm(Station station, Tariff tariff)
        {
            InitializeComponent();
            _station = station;
            _editedTariff = tariff;
            Text = "Редактировать тариф";

            txtDirection.Text = tariff.Direction;
            txtBaseCost.Text = tariff.BaseCost.ToString();

            if (tariff.Strategy is PercentageDiscount discount)
            {
                chkApplyDiscount.Checked = true;
                numDiscountPercent.Value = discount.DiscountPercent;
            }
        }

        private void chkApplyDiscount_CheckedChanged(object sender, EventArgs e)
        {
            numDiscountPercent.Enabled = chkApplyDiscount.Checked;
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(txtDirection.Text))
                    throw new Exception("Направление не может быть пустым");

                if (!double.TryParse(txtBaseCost.Text, out double baseCost) || baseCost < 0)
                    throw new Exception("Введите корректную базовую стоимость (число >= 0)");

                string direction = txtDirection.Text.Trim();

                if (_editedTariff == null)
                {
                    DiscountStrategy strategy = GetStrategyFromForm();

                    _station.AddTariff(direction, baseCost, strategy);
                }
                else
                {
                    if (!_station.ChangeDirection(
                 _station.GetAllTariffs().IndexOf(_editedTariff),
                 direction))
                    {
                        throw new Exception($"Направление '{direction}' уже существует!");
                    }

                    _editedTariff.BaseCost = baseCost;
                    DiscountStrategy newStrategy = GetStrategyFromForm();
                    _editedTariff.SetStrategy(newStrategy);
                    ;
                }

                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка валидации",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private DiscountStrategy GetStrategyFromForm()
        {
            if (chkApplyDiscount.Checked)
            {
                int percent = (int)numDiscountPercent.Value;
                return percent == 0
                    ? (DiscountStrategy)new NoDiscount()
                    : new PercentageDiscount(percent);
            }
            return new NoDiscount();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }
    }
}